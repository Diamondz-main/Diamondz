using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using DnnKolcsonzes.Models;

namespace DnnKolcsonzes.Controllers
{
    public class RentalController : DnnController
    {
        public ActionResult Index(string hccId = "")
        {
            var model = LoadProductFromHotcakes(hccId);

            if (model == null)
            {
                model = new RentalProductViewModel
                {
                    ProductBvin = string.IsNullOrWhiteSpace(hccId) ? "" : hccId,
                    Title = "Termék nem található",
                    Subtitle = "Kölcsönzés",
                    DescriptionHtml = "<p>A megadott termék nem található a Hotcakes adatbázisban.</p>",
                    ImageUrl = "",
                    DailyPrice = 0m,
                    DepositAmount = 120000m,
                    MinRentalDays = 2,
                    MaxRentalDays = 7,
                    PreparationDays = 1,
                    PickupAllowed = true,
                    ShippingAllowed = true,
                    RentalNote = "A kiválasztott időszak mentésre kerül, és sikeres rendelés után lefoglalódik.",
                    Sku = ""
                };
            }

            model.UnavailableRanges = LoadUnavailableRanges(model.ProductBvin, GetCartToken());

            if (model.Highlights == null)
            {
                model.Highlights = new List<RentalHighlightItem>();
            }

            model.Highlights.Clear();

            model.Highlights.Add(new RentalHighlightItem
            {
                Title = "Prémium csomagolás",
                Text = "Biztonságos, elegáns átadás."
            });

            model.Highlights.Add(new RentalHighlightItem
            {
                Title = "Dátumos foglalás",
                Text = "A kiválasztott időszak külön mentésre kerül."
            });

            model.Highlights.Add(new RentalHighlightItem
            {
                Title = "Foglalható dátumok",
                Text = "A friss kosárfoglalások és a kifizetett rendelések is blokkolják a dátumokat."
            });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(AddRentalToCartPostModel post)
        {
            if (post == null || string.IsNullOrWhiteSpace(post.ProductBvin))
            {
                return Redirect(GetBackUrl());
            }

            var model = LoadProductFromHotcakes(post.ProductBvin);
            if (model == null)
            {
                return Content("A termék nem található.");
            }

            var start = ParseDate(post.StartDate);
            var end = ParseDate(post.EndDate);

            if (!start.HasValue || !end.HasValue)
            {
                return Content("Hibás kezdő vagy záró dátum.");
            }

            if (end.Value < start.Value)
            {
                return Content("A záró dátum nem lehet korábbi a kezdő dátumnál.");
            }

            var rentalDays = (end.Value - start.Value).Days + 1;

            if (rentalDays < model.MinRentalDays)
            {
                return Content("Túl rövid bérlés.");
            }

            if (model.MaxRentalDays.HasValue && rentalDays > model.MaxRentalDays.Value)
            {
                return Content("Túl hosszú bérlés.");
            }

            var sku = string.IsNullOrWhiteSpace(post.Sku) ? model.Sku : post.Sku;
            if (string.IsNullOrWhiteSpace(sku))
            {
                return Content("A termék SKU-ja hiányzik, ezért nem rakható a Hotcakes kosárba.");
            }

            var cartToken = EnsureCartToken();

            var blockedRanges = LoadUnavailableRanges(model.ProductBvin, cartToken);
            for (var i = 0; i < blockedRanges.Count; i++)
            {
                var blockedStart = ParseDate(blockedRanges[i].Start);
                var blockedEnd = ParseDate(blockedRanges[i].End);

                if (blockedStart.HasValue && blockedEnd.HasValue)
                {
                    if (start.Value <= blockedEnd.Value && end.Value >= blockedStart.Value)
                    {
                        return Content("A kiválasztott időszak foglalt.");
                    }
                }
            }

            SaveDraftRental(
                model: model,
                sku: sku,
                cartToken: cartToken,
                start: start.Value,
                end: end.Value,
                rentalDays: rentalDays
            );

            var singleAddUrl = BuildHotcakesSingleAddUrl(sku);
            return Content(BuildMultiAddHtml(singleAddUrl, rentalDays), "text/html");
        }

        [HttpGet]
        public ActionResult Ping()
        {
            Response.ContentType = "application/json";
            return Content("{\"ok\":true}", "application/json");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult FinalizeOrder(string orderBvin = "", string orderNumber = "")
        {
            try
            {
                var cartToken = GetCartToken();
                if (string.IsNullOrWhiteSpace(cartToken))
                {
                    return Json(new
                    {
                        ok = false,
                        message = "Nincs cart token."
                    }, JsonRequestBehavior.AllowGet);
                }

                var order = LoadHotcakesOrder(orderBvin, orderNumber);
                if (order == null || string.IsNullOrWhiteSpace(order.OrderBvin))
                {
                    return Json(new
                    {
                        ok = false,
                        message = "A rendelés nem található."
                    }, JsonRequestBehavior.AllowGet);
                }

                var lineItems = LoadHotcakesLineItems(order.OrderBvin);
                if (lineItems == null || lineItems.Count == 0)
                {
                    return Json(new
                    {
                        ok = false,
                        message = "A rendeléshez nem található line item."
                    }, JsonRequestBehavior.AllowGet);
                }

                var updated = FinalizeDraftRowsToOrder(cartToken, order, lineItems);

                return Json(new
                {
                    ok = true,
                    updated = updated,
                    orderBvin = order.OrderBvin,
                    orderNumber = order.OrderNumber
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private RentalProductViewModel LoadProductFromHotcakes(string hccId)
        {
            if (string.IsNullOrWhiteSpace(hccId))
            {
                return null;
            }

            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1
    p.bvin,
    p.Sku,
    p.SitePrice,
    p.ImageFileSmall,
    p.ImageFileMedium,
    t.ProductName,
    t.ShortDescription,
    t.LongDescription
FROM hcc_Product p
LEFT JOIN hcc_ProductTranslations t ON t.ProductId = p.bvin
WHERE p.bvin = @bvin
ORDER BY t.ProductTranslationId";

                cmd.Parameters.AddWithValue("@bvin", hccId);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var productBvin = reader["bvin"] == DBNull.Value ? "" : reader["bvin"].ToString();
                    var title = reader["ProductName"] == DBNull.Value ? "" : reader["ProductName"].ToString();
                    var shortDescription = reader["ShortDescription"] == DBNull.Value ? "" : reader["ShortDescription"].ToString();
                    var longDescription = reader["LongDescription"] == DBNull.Value ? "" : reader["LongDescription"].ToString();
                    var imageFileMedium = reader["ImageFileMedium"] == DBNull.Value ? "" : reader["ImageFileMedium"].ToString();
                    var imageFileSmall = reader["ImageFileSmall"] == DBNull.Value ? "" : reader["ImageFileSmall"].ToString();
                    var sku = reader["Sku"] == DBNull.Value ? "" : reader["Sku"].ToString();

                    var imageFile = !string.IsNullOrWhiteSpace(imageFileMedium) ? imageFileMedium : imageFileSmall;
                    var descriptionHtml = !string.IsNullOrWhiteSpace(longDescription) ? longDescription : shortDescription;

                    if (string.IsNullOrWhiteSpace(descriptionHtml))
                    {
                        descriptionHtml = "<p>Nincs leírás ehhez a termékhez.</p>";
                    }

                    decimal dailyPrice = 0m;
                    if (reader["SitePrice"] != DBNull.Value)
                    {
                        dailyPrice = Convert.ToDecimal(reader["SitePrice"]);
                    }

                    return new RentalProductViewModel
                    {
                        ProductBvin = productBvin,
                        Title = string.IsNullOrWhiteSpace(title) ? "Névtelen termék" : title,
                        Subtitle = "Kölcsönzés",
                        DescriptionHtml = descriptionHtml,
                        ImageUrl = BuildHotcakesImageUrl(productBvin, imageFile),
                        DailyPrice = dailyPrice,
                        DepositAmount = 120000m,
                        MinRentalDays = 2,
                        MaxRentalDays = 7,
                        PreparationDays = 1,
                        PickupAllowed = true,
                        ShippingAllowed = true,
                        RentalNote = "A kiválasztott időszak a rendeléshez kapcsolódik.",
                        Sku = sku
                    };
                }
            }
        }

        private List<UnavailableDateRange> LoadUnavailableRanges(string productBvin, string excludeCartToken)
        {
            var result = new List<UnavailableDateRange>();

            if (string.IsNullOrWhiteSpace(productBvin))
            {
                return result;
            }

            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;
            var draftCutoff = GetStoreNow().AddMinutes(-30);

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT RentalStart, RentalEnd
FROM dbo.Diamondz_RentalBooking
WHERE ProductBvin = @ProductBvin
  AND
  (
      UPPER(ISNULL(Status, '')) IN ('PAID', 'CONFIRMED', 'COMPLETED')
      OR
      (
          UPPER(ISNULL(Status, '')) = 'DRAFT'
          AND LastUpdatedUtc >= @DraftCutoff
          AND (@ExcludeCartToken = '' OR ISNULL(CartToken, '') <> @ExcludeCartToken)
      )
  )
ORDER BY RentalStart";

                cmd.Parameters.AddWithValue("@ProductBvin", productBvin);
                cmd.Parameters.AddWithValue("@DraftCutoff", draftCutoff);
                cmd.Parameters.AddWithValue("@ExcludeCartToken", excludeCartToken ?? "");

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var startValue = reader["RentalStart"] == DBNull.Value
                            ? ""
                            : Convert.ToDateTime(reader["RentalStart"]).ToString("yyyy-MM-dd");

                        var endValue = reader["RentalEnd"] == DBNull.Value
                            ? ""
                            : Convert.ToDateTime(reader["RentalEnd"]).ToString("yyyy-MM-dd");

                        if (!string.IsNullOrWhiteSpace(startValue) && !string.IsNullOrWhiteSpace(endValue))
                        {
                            result.Add(new UnavailableDateRange
                            {
                                Start = startValue,
                                End = endValue
                            });
                        }
                    }
                }
            }

            return result;
        }

        private void SaveDraftRental(
            RentalProductViewModel model,
            string sku,
            string cartToken,
            DateTime start,
            DateTime end,
            int rentalDays)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;
            var nowLocal = GetStoreNow();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var deleteCmd = conn.CreateCommand())
                {
                    deleteCmd.CommandText = @"
DELETE FROM dbo.Diamondz_RentalBooking
WHERE CartToken = @CartToken
  AND ProductBvin = @ProductBvin
  AND Sku = @Sku
  AND UPPER(ISNULL(Status, '')) = 'DRAFT'";

                    deleteCmd.Parameters.AddWithValue("@CartToken", cartToken ?? "");
                    deleteCmd.Parameters.AddWithValue("@ProductBvin", model.ProductBvin ?? "");
                    deleteCmd.Parameters.AddWithValue("@Sku", sku ?? "");
                    deleteCmd.ExecuteNonQuery();
                }

                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.CommandText = @"
INSERT INTO dbo.Diamondz_RentalBooking
(
    ProductBvin,
    OrderBvin,
    OrderNumber,
    CartToken,
    Sku,
    VariantId,
    LineItemId,
    CustomerEmail,
    CustomerName,
    RentalStart,
    RentalEnd,
    Quantity,
    DailyPrice,
    TotalPrice,
    Status,
    InternalNotes,
    CreatedOnUtc,
    LastUpdatedUtc
)
VALUES
(
    @ProductBvin,
    NULL,
    NULL,
    @CartToken,
    @Sku,
    NULL,
    NULL,
    @CustomerEmail,
    @CustomerName,
    @RentalStart,
    @RentalEnd,
    @Quantity,
    @DailyPrice,
    @TotalPrice,
    @Status,
    @InternalNotes,
    @CreatedOnLocal,
    @LastUpdatedLocal
)";

                    insertCmd.Parameters.AddWithValue("@ProductBvin", string.IsNullOrWhiteSpace(model.ProductBvin) ? (object)DBNull.Value : model.ProductBvin);
                    insertCmd.Parameters.AddWithValue("@CartToken", string.IsNullOrWhiteSpace(cartToken) ? (object)DBNull.Value : cartToken);
                    insertCmd.Parameters.AddWithValue("@Sku", string.IsNullOrWhiteSpace(sku) ? (object)DBNull.Value : sku);
                    insertCmd.Parameters.AddWithValue("@CustomerEmail", string.IsNullOrWhiteSpace(GetCurrentCustomerEmail()) ? (object)DBNull.Value : GetCurrentCustomerEmail());
                    insertCmd.Parameters.AddWithValue("@CustomerName", string.IsNullOrWhiteSpace(GetCurrentCustomerName()) ? (object)DBNull.Value : GetCurrentCustomerName());
                    insertCmd.Parameters.AddWithValue("@RentalStart", start);
                    insertCmd.Parameters.AddWithValue("@RentalEnd", end);
                    insertCmd.Parameters.AddWithValue("@Quantity", rentalDays);
                    insertCmd.Parameters.AddWithValue("@DailyPrice", model.DailyPrice);
                    insertCmd.Parameters.AddWithValue("@TotalPrice", model.DailyPrice * rentalDays);
                    insertCmd.Parameters.AddWithValue("@Status", "Draft");
                    insertCmd.Parameters.AddWithValue("@InternalNotes", "Kosárba rakva, rendelésre vár.");
                    insertCmd.Parameters.AddWithValue("@CreatedOnLocal", nowLocal);
                    insertCmd.Parameters.AddWithValue("@LastUpdatedLocal", nowLocal);

                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private int FinalizeDraftRowsToOrder(string cartToken, HotcakesOrderInfo order, List<HotcakesLineItemInfo> lineItems)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;
            var updatedCount = 0;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var drafts = LoadDraftRowsForCart(conn, cartToken);

                foreach (var lineItem in lineItems)
                {
                    var matchingDraft = drafts
                        .FirstOrDefault(x =>
                            string.IsNullOrWhiteSpace(x.OrderBvin) &&
                            SafeEquals(x.Sku, lineItem.Sku) &&
                            x.Quantity == lineItem.Quantity);

                    if (matchingDraft == null)
                    {
                        continue;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE dbo.Diamondz_RentalBooking
SET
    OrderBvin = @OrderBvin,
    OrderNumber = @OrderNumber,
    LineItemId = @LineItemId,
    CustomerEmail = @CustomerEmail,
    CustomerName = @CustomerName,
    Status = @Status,
    InternalNotes = @InternalNotes,
    LastUpdatedUtc = GETUTCDATE()
WHERE Id = @Id";

                        cmd.Parameters.AddWithValue("@OrderBvin", (object)(order.OrderBvin ?? "") ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OrderNumber", (object)(order.OrderNumber ?? "") ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LineItemId", (object)(lineItem.LineItemId ?? "") ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerEmail", (object)(order.CustomerEmail ?? "") ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerName", (object)(order.CustomerName ?? "") ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", "Paid");
                        cmd.Parameters.AddWithValue("@InternalNotes", "Rendeléshez kapcsolva.");
                        cmd.Parameters.AddWithValue("@Id", matchingDraft.Id);

                        var affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            updatedCount++;
                        }
                    }

                    matchingDraft.OrderBvin = order.OrderBvin;
                }
            }

            return updatedCount;
        }

        private List<RentalDraftRow> LoadDraftRowsForFinalize(SqlConnection conn, string cartToken, HotcakesOrderInfo order, List<HotcakesLineItemInfo> lineItems)
        {
            var drafts = LoadDraftRowsForCart(conn, cartToken);

            if (drafts.Count > 0)
            {
                return drafts;
            }

            drafts = LoadDraftRowsByRecentSkus(conn, lineItems);

            if (order != null && !string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                drafts = drafts
                    .Where(x => string.IsNullOrWhiteSpace(x.CustomerEmail) || SafeEquals(x.CustomerEmail, order.CustomerEmail))
                    .ToList();
            }

            return drafts;
        }

        private List<RentalDraftRow> LoadDraftRowsForCart(SqlConnection conn, string cartToken)
        {
            var result = new List<RentalDraftRow>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    Id,
    OrderBvin,
    ProductBvin,
    CartToken,
    Sku,
    Quantity,
    CustomerEmail
FROM dbo.Diamondz_RentalBooking
WHERE CartToken = @CartToken
  AND UPPER(ISNULL(Status, '')) = 'DRAFT'
ORDER BY Id";

                cmd.Parameters.AddWithValue("@CartToken", cartToken ?? "");

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new RentalDraftRow
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            OrderBvin = reader["OrderBvin"] == DBNull.Value ? "" : reader["OrderBvin"].ToString(),
                            ProductBvin = reader["ProductBvin"] == DBNull.Value ? "" : reader["ProductBvin"].ToString(),
                            CartToken = reader["CartToken"] == DBNull.Value ? "" : reader["CartToken"].ToString(),
                            Sku = reader["Sku"] == DBNull.Value ? "" : reader["Sku"].ToString(),
                            Quantity = reader["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Quantity"]),
                            CustomerEmail = reader["CustomerEmail"] == DBNull.Value ? "" : reader["CustomerEmail"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        private List<RentalDraftRow> LoadDraftRowsByRecentSkus(SqlConnection conn, List<HotcakesLineItemInfo> lineItems)
        {
            var result = new List<RentalDraftRow>();

            if (lineItems == null || lineItems.Count == 0)
            {
                return result;
            }

            var skuList = lineItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
                .Select(x => x.Sku.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (skuList.Count == 0)
            {
                return result;
            }

            var minDate = GetStoreNow().AddDays(-3);

            using (var cmd = conn.CreateCommand())
            {
                var skuParams = new List<string>();

                for (var i = 0; i < skuList.Count; i++)
                {
                    var paramName = "@Sku" + i;
                    skuParams.Add(paramName);
                    cmd.Parameters.AddWithValue(paramName, skuList[i]);
                }

                cmd.Parameters.AddWithValue("@MinDate", minDate);

                cmd.CommandText = @"
SELECT
    Id,
    OrderBvin,
    ProductBvin,
    CartToken,
    Sku,
    Quantity,
    CustomerEmail
FROM dbo.Diamondz_RentalBooking
WHERE UPPER(ISNULL(Status, '')) = 'DRAFT'
  AND LastUpdatedUtc >= @MinDate
  AND Sku IN (" + string.Join(",", skuParams) + @")
ORDER BY Id";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new RentalDraftRow
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            OrderBvin = reader["OrderBvin"] == DBNull.Value ? "" : reader["OrderBvin"].ToString(),
                            ProductBvin = reader["ProductBvin"] == DBNull.Value ? "" : reader["ProductBvin"].ToString(),
                            CartToken = reader["CartToken"] == DBNull.Value ? "" : reader["CartToken"].ToString(),
                            Sku = reader["Sku"] == DBNull.Value ? "" : reader["Sku"].ToString(),
                            Quantity = reader["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Quantity"]),
                            CustomerEmail = reader["CustomerEmail"] == DBNull.Value ? "" : reader["CustomerEmail"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        private List<RentalDraftRow> GetDraftRowsForCurrentCart(string cartToken)
        {
            var result = new List<RentalDraftRow>();
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                result = LoadDraftRowsForCart(conn, cartToken);
            }

            return result;
        }

        private HotcakesOrderInfo LoadHotcakesOrder(string orderBvin, string orderNumber)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var columns = GetTableColumns(conn, "hcc_Order");

                var idCol = FirstExisting(columns, "bvin", "Bvin", "OrderBvin");
                var numberCol = FirstExisting(columns, "OrderNumber", "ordernumber");
                var emailCol = FirstExisting(columns, "UserEmail", "CustomerEmail", "Email");
                var firstNameCol = FirstExisting(columns, "BillingFirstName", "FirstName");
                var lastNameCol = FirstExisting(columns, "BillingLastName", "LastName");

                if (string.IsNullOrWhiteSpace(idCol))
                {
                    return null;
                }

                var whereSql = "";
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(orderBvin))
                {
                    whereSql = "[" + idCol + "] = @OrderBvin";
                    parameters.Add(new SqlParameter("@OrderBvin", orderBvin));
                }
                else if (!string.IsNullOrWhiteSpace(orderNumber) && !string.IsNullOrWhiteSpace(numberCol))
                {
                    whereSql = "[" + numberCol + "] = @OrderNumber";
                    parameters.Add(new SqlParameter("@OrderNumber", orderNumber));
                }
                else
                {
                    return null;
                }

                var selectParts = new List<string>
                {
                    "[" + idCol + "] AS OrderBvin"
                };

                if (!string.IsNullOrWhiteSpace(numberCol))
                    selectParts.Add("[" + numberCol + "] AS OrderNumber");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(50)) AS OrderNumber");

                if (!string.IsNullOrWhiteSpace(emailCol))
                    selectParts.Add("[" + emailCol + "] AS CustomerEmail");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS CustomerEmail");

                if (!string.IsNullOrWhiteSpace(firstNameCol))
                    selectParts.Add("[" + firstNameCol + "] AS FirstName");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS FirstName");

                if (!string.IsNullOrWhiteSpace(lastNameCol))
                    selectParts.Add("[" + lastNameCol + "] AS LastName");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS LastName");

                var sql = @"
SELECT TOP 1
    " + string.Join("," + Environment.NewLine + "    ", selectParts) + @"
FROM hcc_Order
WHERE " + whereSql;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (var p in parameters)
                    {
                        cmd.Parameters.Add(p);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        var firstName = reader["FirstName"] == DBNull.Value ? "" : reader["FirstName"].ToString();
                        var lastName = reader["LastName"] == DBNull.Value ? "" : reader["LastName"].ToString();
                        var fullName = (firstName + " " + lastName).Trim();

                        return new HotcakesOrderInfo
                        {
                            OrderBvin = reader["OrderBvin"] == DBNull.Value ? "" : reader["OrderBvin"].ToString(),
                            OrderNumber = reader["OrderNumber"] == DBNull.Value ? "" : reader["OrderNumber"].ToString(),
                            CustomerEmail = reader["CustomerEmail"] == DBNull.Value ? "" : reader["CustomerEmail"].ToString(),
                            CustomerName = fullName
                        };
                    }
                }
            }
        }

        private HotcakesOrderInfo LoadRecentHotcakesOrderByDrafts(List<RentalDraftRow> drafts)
        {
            if (drafts == null || drafts.Count == 0)
            {
                return null;
            }

            var currentEmail = GetCurrentCustomerEmail();
            var recentOrders = LoadRecentHotcakesOrders(20, currentEmail);

            foreach (var order in recentOrders)
            {
                var lineItems = LoadHotcakesLineItems(order.OrderBvin);

                var matches = lineItems.Any(li =>
                    drafts.Any(d =>
                        (!string.IsNullOrWhiteSpace(d.Sku) && SafeEquals(d.Sku, li.Sku)) ||
                        (!string.IsNullOrWhiteSpace(d.ProductBvin) && !string.IsNullOrWhiteSpace(li.ProductBvin) && SafeEquals(d.ProductBvin, li.ProductBvin))
                    ));

                if (matches)
                {
                    return order;
                }
            }

            return null;
        }

        private List<HotcakesOrderInfo> LoadRecentHotcakesOrders(int top, string emailFilter)
        {
            var result = new List<HotcakesOrderInfo>();
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var columns = GetTableColumns(conn, "hcc_Order");

                var idCol = FirstExisting(columns, "bvin", "Bvin", "OrderBvin");
                var numberCol = FirstExisting(columns, "OrderNumber", "ordernumber");
                var emailCol = FirstExisting(columns, "UserEmail", "CustomerEmail", "Email");
                var firstNameCol = FirstExisting(columns, "BillingFirstName", "FirstName");
                var lastNameCol = FirstExisting(columns, "BillingLastName", "LastName");
                var timeCol = FirstExisting(columns, "TimeOfOrderUtc", "TimeOfOrder", "LastUpdatedUtc", "LastUpdated", "CreationDate", "CreatedOnDate");

                if (string.IsNullOrWhiteSpace(idCol))
                {
                    return result;
                }

                var selectParts = new List<string>
                {
                    "[" + idCol + "] AS OrderBvin"
                };

                if (!string.IsNullOrWhiteSpace(numberCol))
                    selectParts.Add("[" + numberCol + "] AS OrderNumber");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(50)) AS OrderNumber");

                if (!string.IsNullOrWhiteSpace(emailCol))
                    selectParts.Add("[" + emailCol + "] AS CustomerEmail");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS CustomerEmail");

                if (!string.IsNullOrWhiteSpace(firstNameCol))
                    selectParts.Add("[" + firstNameCol + "] AS FirstName");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS FirstName");

                if (!string.IsNullOrWhiteSpace(lastNameCol))
                    selectParts.Add("[" + lastNameCol + "] AS LastName");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(255)) AS LastName");

                string whereClause = "";
                if (!string.IsNullOrWhiteSpace(emailFilter) && !string.IsNullOrWhiteSpace(emailCol))
                {
                    whereClause = "WHERE [" + emailCol + "] = @Email";
                }

                var orderByClause = !string.IsNullOrWhiteSpace(timeCol)
                    ? "[" + timeCol + "] DESC"
                    : "[" + idCol + "] DESC";

                var sql = @"
SELECT TOP " + Math.Max(1, top).ToString(CultureInfo.InvariantCulture) + @"
    " + string.Join("," + Environment.NewLine + "    ", selectParts) + @"
FROM hcc_Order
" + whereClause + @"
ORDER BY " + orderByClause;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;

                    if (!string.IsNullOrWhiteSpace(whereClause))
                    {
                        cmd.Parameters.AddWithValue("@Email", emailFilter);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var firstName = reader["FirstName"] == DBNull.Value ? "" : reader["FirstName"].ToString();
                            var lastName = reader["LastName"] == DBNull.Value ? "" : reader["LastName"].ToString();
                            var fullName = (firstName + " " + lastName).Trim();

                            result.Add(new HotcakesOrderInfo
                            {
                                OrderBvin = reader["OrderBvin"] == DBNull.Value ? "" : reader["OrderBvin"].ToString(),
                                OrderNumber = reader["OrderNumber"] == DBNull.Value ? "" : reader["OrderNumber"].ToString(),
                                CustomerEmail = reader["CustomerEmail"] == DBNull.Value ? "" : reader["CustomerEmail"].ToString(),
                                CustomerName = fullName
                            });
                        }
                    }
                }
            }

            return result;
        }

        private List<HotcakesLineItemInfo> LoadHotcakesLineItems(string orderBvin)
        {
            var result = new List<HotcakesLineItemInfo>();
            var connectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var columns = GetTableColumns(conn, "hcc_LineItem");

                var idCol = FirstExisting(columns, "Id", "LineItemId", "bvin", "Bvin");
                var orderBvinCol = FirstExisting(columns, "OrderBvin", "orderbvin");
                var skuCol = FirstExisting(columns, "ProductSku", "Sku");
                var productBvinCol = FirstExisting(columns, "ProductId", "ProductBvin");
                var quantityCol = FirstExisting(columns, "Quantity", "quantity");

                if (string.IsNullOrWhiteSpace(orderBvinCol) || string.IsNullOrWhiteSpace(skuCol) || string.IsNullOrWhiteSpace(quantityCol))
                {
                    return result;
                }

                var selectParts = new List<string>();

                if (!string.IsNullOrWhiteSpace(idCol))
                    selectParts.Add("CAST([" + idCol + "] AS NVARCHAR(100)) AS LineItemId");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(100)) AS LineItemId");

                selectParts.Add("[" + skuCol + "] AS Sku");

                if (!string.IsNullOrWhiteSpace(productBvinCol))
                    selectParts.Add("CAST([" + productBvinCol + "] AS NVARCHAR(100)) AS ProductBvin");
                else
                    selectParts.Add("CAST('' AS NVARCHAR(100)) AS ProductBvin");

                selectParts.Add("CAST([" + quantityCol + "] AS INT) AS Quantity");

                var sql = @"
SELECT
    " + string.Join("," + Environment.NewLine + "    ", selectParts) + @"
FROM hcc_LineItem
WHERE [" + orderBvinCol + @"] = @OrderBvin
ORDER BY " + (!string.IsNullOrWhiteSpace(idCol) ? "[" + idCol + "]" : "[" + skuCol + "]");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@OrderBvin", orderBvin);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new HotcakesLineItemInfo
                            {
                                LineItemId = reader["LineItemId"] == DBNull.Value ? "" : reader["LineItemId"].ToString(),
                                Sku = reader["Sku"] == DBNull.Value ? "" : reader["Sku"].ToString(),
                                ProductBvin = reader["ProductBvin"] == DBNull.Value ? "" : reader["ProductBvin"].ToString(),
                                Quantity = reader["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Quantity"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        private HashSet<string> GetTableColumns(SqlConnection conn, string tableName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = @TableName";

                cmd.Parameters.AddWithValue("@TableName", tableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader["COLUMN_NAME"] == DBNull.Value ? "" : reader["COLUMN_NAME"].ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            result.Add(name);
                        }
                    }
                }
            }

            return result;
        }

        private string FirstExisting(HashSet<string> columns, params string[] candidates)
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                if (columns.Contains(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return "";
        }

        private DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime dt;
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                return dt.Date;
            }

            return null;
        }

        private string BuildHotcakesImageUrl(string productBvin, string fileName)
        {
            if (string.IsNullOrWhiteSpace(productBvin) || string.IsNullOrWhiteSpace(fileName))
            {
                return "";
            }

            var portalId = 0;
            if (PortalSettings.Current != null)
            {
                portalId = PortalSettings.Current.PortalId;
            }

            return "/Portals/" + portalId + "/Hotcakes/Data/products/" + productBvin + "/medium/" + fileName;
        }

        private string BuildHotcakesSingleAddUrl(string sku)
        {
            var cartUrl = "/Cart";
            return cartUrl + "?AddSku=" + HttpUtility.UrlEncode(sku);
        }

        private string BuildMultiAddHtml(string addUrl, int quantity)
        {
            var safeAddUrl = HttpUtility.JavaScriptStringEncode(addUrl);
            var safeCartUrl = HttpUtility.JavaScriptStringEncode("/Cart");

            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Kosár frissítése...</title>
</head>
<body>
    <div style=""font-family:Arial,sans-serif;padding:20px;"">Kosár frissítése...</div>

    <script>
        (function () {
            var addUrl = '" + safeAddUrl + @"';
            var cartUrl = '" + safeCartUrl + @"';
            var quantity = " + quantity.ToString(CultureInfo.InvariantCulture) + @";

            function goToCart() {
                window.location.href = cartUrl;
            }

            function addNext(index) {
                if (index >= quantity) {
                    goToCart();
                    return;
                }

                fetch(addUrl, {
                    method: 'GET',
                    credentials: 'same-origin',
                    cache: 'no-store',
                    redirect: 'follow'
                })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('HTTP ' + response.status);
                    }
                    return response.text();
                })
                .then(function () {
                    addNext(index + 1);
                })
                .catch(function (err) {
                    console.error('Hotcakes add to cart hiba:', err);
                    goToCart();
                });
            }

            addNext(0);
        })();
    </script>
</body>
</html>";
        }

        private string EnsureCartToken()
        {
            var existing = GetCartToken();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            var token = Guid.NewGuid().ToString("N");
            var cookie = new HttpCookie("diamondz_rental_cart_token", token)
            {
                HttpOnly = true
            };

            Response.Cookies.Add(cookie);
            return token;
        }

        private string GetCartToken()
        {
            var cookie = Request != null ? Request.Cookies["diamondz_rental_cart_token"] : null;
            return cookie != null ? cookie.Value : "";
        }

        private string GetCurrentCustomerEmail()
        {
            var user = UserController.Instance.GetCurrentUserInfo();
            if (user != null && user.UserID > 0 && !string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }

            return "";
        }

        private string GetCurrentCustomerName()
        {
            var user = UserController.Instance.GetCurrentUserInfo();
            if (user != null && user.UserID > 0)
            {
                var name = (user.FirstName + " " + user.LastName).Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    return user.DisplayName;
                }
            }

            return "";
        }

        private bool SafeEquals(string a, string b)
        {
            return string.Equals(
                (a ?? "").Trim(),
                (b ?? "").Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private string GetBackUrl()
        {
            var referrer = Request != null && Request.UrlReferrer != null
                ? Request.UrlReferrer.ToString()
                : "/";

            return referrer;
        }

        private DateTime GetStoreNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(2);
            }
        }

        private class RentalDraftRow
        {
            public int Id { get; set; }
            public string OrderBvin { get; set; }
            public string ProductBvin { get; set; }
            public string CartToken { get; set; }
            public string Sku { get; set; }
            public int Quantity { get; set; }
            public string CustomerEmail { get; set; }
        }

        private class HotcakesOrderInfo
        {
            public string OrderBvin { get; set; }
            public string OrderNumber { get; set; }
            public string CustomerEmail { get; set; }
            public string CustomerName { get; set; }
        }

        private class HotcakesLineItemInfo
        {
            public string LineItemId { get; set; }
            public string Sku { get; set; }
            public string ProductBvin { get; set; }
            public int Quantity { get; set; }
        }
    }
}