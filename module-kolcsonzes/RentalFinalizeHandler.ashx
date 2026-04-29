<%@ WebHandler Language="C#" Class="DnnKolcsonzes.RentalFinalizeHandler" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace DnnKolcsonzes
{
    public class RentalFinalizeHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        private class LineItemRow
        {
            public string Sku { get; set; }
            public int Qty { get; set; }
            public long LineItemId { get; set; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            var orderBvinRaw = (context.Request.QueryString["orderBvin"] ?? "").Trim();

            if (string.IsNullOrWhiteSpace(orderBvinRaw))
            {
                context.Response.Write("{\"ok\":false,\"message\":\"Hiányzó orderBvin.\"}");
                return;
            }

            Guid orderGuid;
            if (!Guid.TryParse(orderBvinRaw, out orderGuid))
            {
                context.Response.Write("{\"ok\":false,\"message\":\"Érvénytelen orderBvin formátum: " + orderBvinRaw + "\"}");
                return;
            }

            try
            {
                var connStr = ConfigurationManager
                    .ConnectionStrings["SiteSqlServer"].ConnectionString;

                string orderNumber = "";
                string customerEmail = "";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1
    OrderNumber,
    UserEmail
FROM hcc_Order
WHERE bvin = @bvin";
                    cmd.Parameters.Add("@bvin", SqlDbType.UniqueIdentifier).Value = orderGuid;
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            orderNumber = r["OrderNumber"] == DBNull.Value ? "" : r["OrderNumber"].ToString();
                            customerEmail = r["UserEmail"] == DBNull.Value ? "" : r["UserEmail"].ToString();
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    context.Response.Write("{\"ok\":false,\"message\":\"Rendelés nem található.\"}");
                    return;
                }

                var lineItems = new List<LineItemRow>();

                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT 
    Id AS LineItemId,
    ProductSku,
    CAST(Quantity AS INT) AS Qty
FROM hcc_LineItem
WHERE OrderBvin = @bvin";
                        cmd.Parameters.Add("@bvin", SqlDbType.UniqueIdentifier).Value = orderGuid;
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                lineItems.Add(new LineItemRow
                                {
                                    LineItemId = r["LineItemId"] == DBNull.Value ? 0 : Convert.ToInt64(r["LineItemId"]),
                                    Sku = r["ProductSku"] == DBNull.Value ? "" : r["ProductSku"].ToString(),
                                    Qty = r["Qty"] == DBNull.Value ? 0 : Convert.ToInt32(r["Qty"])
                                });
                            }
                        }
                    }

                    int updated = 0;

                    foreach (var li in lineItems)
                    {
                        if (string.IsNullOrWhiteSpace(li.Sku))
                        {
                            continue;
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
UPDATE TOP (1) dbo.Diamondz_RentalBooking
SET
    OrderBvin       = @OrderBvin,
    OrderNumber     = @OrderNumber,
    LineItemId      = @LineItemId,
    CustomerEmail   = @CustomerEmail,
    Status          = 'Paid',
    InternalNotes   = 'Rendeléshez kapcsolva.',
    ModifiedOnUtc   = GETUTCDATE(),
    LastUpdatedUtc  = GETUTCDATE()
WHERE Sku = @Sku
  AND Quantity = @Qty
  AND UPPER(ISNULL(Status, '')) = 'DRAFT'
  AND OrderBvin IS NULL";

                            cmd.Parameters.Add("@OrderBvin", SqlDbType.UniqueIdentifier).Value = orderGuid;
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                            cmd.Parameters.Add("@LineItemId", SqlDbType.BigInt).Value = li.LineItemId;
                            cmd.Parameters.AddWithValue("@CustomerEmail", customerEmail);
                            cmd.Parameters.AddWithValue("@Sku", li.Sku);
                            cmd.Parameters.AddWithValue("@Qty", li.Qty);

                            updated += cmd.ExecuteNonQuery();
                        }
                    }

                    context.Response.Write(
                        "{\"ok\":true,\"updated\":" + updated + ",\"orderNumber\":\"" + orderNumber + "\"}");
                }
            }
            catch (Exception ex)
            {
                context.Response.Write(
                    "{\"ok\":false,\"message\":\"" + ex.Message.Replace("\"", "'") + "\"}");
            }
        }
    }
}
