# Sales Order Integration

## Overview
This integration software facilitates the creation of Sales Orders (SOs) through a structured process involving customer data entry, item selection, and order generation. It ensures accurate data retrieval and storage while maintaining a smooth user experience.

## Process Flow

### 1. Customer Page
- User enters the customer name and presses `Tab`.
- Customer Account and Delivery Address auto-fill.
- User selects `Site` from a dropdown (Fetched via API).
- User selects `Warehouse` (Fetched via API based on Site selection).
- "Create Items" button is enabled once all necessary data is filled.
- Optionally, the user can add a `Customer Reference`.
- Customer data is saved in `Session Storage`.
- Clicking "Create Items" navigates to the Items Page.

### 2. Items Page
- Warehouses are fetched dynamically based on the selected `Site`.
- Locations are fetched dynamically based on the selected `Warehouse`.
- The `Site` dropdown is pre-selected with the value from customer data (if available).
- Items are loaded from a file and displayed in a table.
- The user can search for items.
- Selecting an item fetches its `Unit` from the backend and syncs it with `P. Unit`.
- User enters `Quantity` and presses `Tab`, which sets the `Packing Quantity` accordingly.
- The `Master Unit` is fetched from the backend, and the `Master Quantity` is calculated.
- Selected items are sorted to appear at the top.
- Selected items are saved in `Session Storage`.
- Clicking "Generate SO" navigates to the Checkout Page.

### 3. Checkout Page
- Customer and item data are retrieved from `Session Storage`.
- The table is populated with the fetched data.
- Clicking "Generate Sales Order" collects `customerData` and `itemsSelected` into `orderData`.
- For each selected item, the `createSO` function is called.
- Sales Order ID is fetched and incremented from the sequence.
- Missing `RECID` values are identified and filled using the smallest available number.
- A new record is created in:
  1. `Sales Order`
  2. `Sales Line`
  3. `Invent Trans`
- Toast notifications display the generated Sales Order IDs.

## RECID Calculation
To ensure unique `RECID` values, the system calculates the missing `RECID` using a self-join query. The query finds the smallest available `RECID` that is not currently assigned:
```sql
WITH MissingNumbers AS (
    SELECT t1.RECID + 1 AS MissingID
    FROM {tableName} t1
    LEFT JOIN {tableName} t2 ON t1.RECID + 1 = t2.RECID
    WHERE t2.RECID IS NULL
)
SELECT TOP 1 MissingID 
FROM MissingNumbers
WHERE MissingID NOT IN (SELECT RECID FROM {tableName})
ORDER BY MissingID ASC;
```
This ensures that each new Sales Order receives a unique `RECID` without gaps.

## Data Stored in Session

### customerData Example:
```json
{
  "name": "S&D",
  "customerAccount": "CUST-001297",
  "deliveryAddress": "Karachi. PK",
  "podDate": "2025-02-26",
  "customerRequisition": "",
  "reference": "",
  "site": "MATCO02",
  "warehouse": "AA-02"
}
```

### itemsSelected Example:
```json
[
  {
    "itemNumber": "ITM-019497",
    "itemName": "FALAK BROWN BASMATI 1 KG",
    "site": "MATCO02",
    "warehouse": "AA-02"
  },
  {
    "itemNumber": "ITM-016037",
    "itemName": "FALAK JASMINE RICE 5KG",
    "site": "MATCO02",
    "warehouse": "AA-02"
  },
  {
    "itemNumber": "ITM-010550",
    "itemName": "Falak Jasmine Rice 1kg",
    "site": "MATCO02",
    "warehouse": "AA-02"
  },
  {
    "itemNumber": "ITM-016063",
    "itemName": "FALAK EASY COOK SELLA 10KG",
    "site": "MATCO02",
    "warehouse": "AA-02"
  }
]
```

## Technologies Used
- **Frontend:** Bootstrap, JavaScript
- **Backend:** C# (ASP.NET Core), Ajax for the win!
- **Database:** Microsoft Dynamics AX 2009
- **Data Storage:** Session Storage/Dynamics Internal DB

## Installation
1. Clone the repository:
   ```sh
   git clone https://github.com/mohsinziaa/Dynamics-AX-Integration.git
   ```
2. Navigate to the project directory:
   ```sh
   cd Dynamics-AX-Integration
   ```
3. Configure the API endpoints and database connection settings in the `.env` file or configuration file.
4. Run the application:
   ```sh
   dotnet run
   ```

## Usage
1. Enter customer details on the Customer Page.
2. Select the Site and Warehouse.
3. Click "Create Items" to proceed.
4. Select items from the Items Page.
5. Click "Generate SO" to navigate to Checkout.
6. Click "Generate Sales Order" to finalize and store the order.

## License
This project is licensed under MATCO FOODS LIMITED (MFL).

