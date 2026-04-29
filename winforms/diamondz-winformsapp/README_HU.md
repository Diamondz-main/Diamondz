# Diamondz WinForms starter

## Indítás
1. Nyisd meg a `DiamondzWinForms.csproj` fájlt Visual Studio-ban.
2. Az `ApiSettings.cs` fájlban töltsd ki:
   - `BaseUrl`
   - `ApiKey`
3. Indítsd el az alkalmazást.

## Mit tud most?
- Dashboard oldal
- Termékek lista a products végpontról
- Rendelések lista az orders végpontról
- Frissítés gomb

## Fontos
A most feltöltött orders minta alapján ezek biztosan benne vannak:
- rendelés azonosító / sorszám
- vásárló email
- billing cím
- státusz
- rendelés ideje
- végösszeg

A mintában **nem látszik** kölcsönzési intervallum és külön terméklista, ezért a képen lévő "Apr 10 - Apr 17" típusú mezőt és a terméknevet az orders táblában majd akkor érdemes bekötni, ha:
- az orders API visszaadja ezeket, vagy
- külön custom propertyből kiolvassuk, vagy
- lesz külön rental endpoint.
