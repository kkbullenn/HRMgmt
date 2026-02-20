# Test Data

This folder contains test data files used by the test suite.

## Files Overview

1. **test_employees_shifts.sql** - Insert statements for test data
2. **cleanup_test_data.sql** - Cleanup script to remove test data
3. **test_data_mapping.md** - Detailed mapping of test cases to their data requirements

## test_employees_shifts.sql

SQL insert statements for test employees and shifts used in Sam's blackbox test cases.

### Usage

After running database migrations, execute this SQL file to populate the test data:

```bash
# For SQLite (if using SQLite)
sqlite3 your_database.db < testData/test_employees_shifts.sql

# For SQL Server
sqlcmd -S localhost -d HRMgmt -i testData/test_employees_shifts.sql

# Or use your database management tool to run the script
```

### Test Employees

- **E001** (Emily Anderson) - Used in TC-002, TC-008, TC-012
- **E002** (Michael Brown) - Used in TC-002
- **E003** (Sarah Chen) - Used in TC-002
- **E005** (James Wilson) - Used in TC-005
- **E006** (Jennifer Taylor) - Used in TC-005
- **E007** (David Martinez) - Used in TC-005
- **E008** (Jessica Lee) - Used in TC-007

### Test Shifts

- **D1** Morning (08:00-16:00) - Used in TC-008, TC-012
- **D2** Day (09:00-17:00) - Used in TC-002
- **D4** Afternoon (12:00-17:00) - Used in TC-005
- **D5** Mid-Morning (10:00-13:30) - Used in TC-005
- **D6** Mid-Afternoon (13:30-19:00) - Used in TC-005
- **D7** Early (07:00-15:00) - Used in TC-007 (overlaps with D8)
- **D8** Overlapping (10:00-18:00) - Used in TC-007 (overlaps with D7)

## cleanup_test_data.sql

Script to remove all test data from the database after testing is complete.

```bash
# Run cleanup
sqlcmd -S localhost -d HRMgmt -i testData/cleanup_test_data.sql
```

## test_data_mapping.md

Comprehensive documentation of:
- Which test cases use which employees and shifts
- Complete GUID mappings
- Cleanup procedures
- Database setup instructions

## Notes

- All employee GUIDs follow the pattern: `11111111-1111-1111-1111-00000000000X`
- All shift GUIDs follow the pattern: `22222222-2222-2222-2222-00000000000X`
- These patterns make test data easy to identify and clean up
- All test files reference these exact GUIDs
- Test data is shared across multiple test cases where appropriate (e.g., E001 is used in 3 tests)
