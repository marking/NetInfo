**1. Success Rate per Host:**

```sql
SELECT
    Host,
    CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
    COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host;
```

*   **`SELECT Host`**:  This selects the `Host` column to group by.
*   **`CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate`**: This calculates the success rate.
    *   `SUM(Success)`: Sums all `Success` values (1 for success, 0 for failure).
    *   `COUNT(*)`: Counts the total number of pings for that host.
    *   `CAST(SUM(Success) AS REAL)`: Casts the sum to a `REAL` number so the division produces a decimal (otherwise, SQLite would do integer division).
    *   `SuccessRate`: The alias given to this calculated column.
*   **`COUNT(*) AS TotalPings`**: This counts the total number of pings per host and alias it to TotalPings.
*   **`FROM PingResults`**: Specifies the table.
*   **`GROUP BY Host`**:  Groups the results by unique `Host` values.

**2. Success Rate per Host and Hop:**

```sql
SELECT
    Host,
    Hop,
    CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
        COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host, Hop;
```

*   This query is similar to the first one, but it adds `Hop` to the `SELECT` clause and the `GROUP BY` clause.  This will give you the success rate for each host *and* each hop within that host's traces.

**3. Success Rate per Host per Day**
```sql
SELECT
    Host,
    DATE(DateTime) as PingDate,
     CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
        COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host, PingDate;
```
* **`DATE(DateTime) as PingDate`**: Extracts the date portion from the `DateTime` column, this assumes the date time format can be converted by SQLite's `DATE()` function, else use proper format to extract the date. This will group the results on host and the date on which the ping was sent
* Rest of the query is similar to the ones described above.

**Putting it all together as a single query (using subqueries to keep it cleaner):**

```sql
SELECT
    'Host Stats' as StatsType,
    Host,
    NULL AS Hop,
    NULL AS PingDate,
    CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
        COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host
UNION ALL
SELECT
    'Host/Hop Stats' as StatsType,
    Host,
    Hop,
    NULL AS PingDate,
    CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
        COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host, Hop
UNION ALL
SELECT
    'Host/Day Stats' as StatsType,
    Host,
    NULL AS Hop,
    DATE(DateTime) as PingDate,
    CAST(SUM(Success) AS REAL) / COUNT(*) AS SuccessRate,
    COUNT(*) AS TotalPings
FROM PingResults
GROUP BY Host, PingDate;
```

This version combines all the stats into a single result set for your ease to interpret, it creates a column `StatsType` which tells us what kind of stats are in that row.

**Important Considerations:**

*   **Error Handling:** You might want to add checks for cases where `COUNT(*)` is 0 to avoid division by zero errors. You can use `CASE WHEN COUNT(*) > 0 THEN ... ELSE 0 END` to handle these.
*   **Date/Time Format:** The `DATE(DateTime)` assumes your DateTime format is one that SQLite can parse. If that is not the case, you might need to use date/time functions that can work with your format, or standardize on date format.
*   **Performance:** For very large tables, you might consider creating a materialized view if you need to perform these queries often and optimize them using indexes
*   **Data Presentation:** Depending on how you want to present this information, you might want to format it further in your application (e.g., as percentages).
