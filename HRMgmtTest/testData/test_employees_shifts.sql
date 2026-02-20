INSERT INTO Roles
    (Id, RoleName)
VALUES (1, 'Employee');

INSERT INTO Roles
    (Id, RoleName)
VALUES (2, 'HR');

INSERT INTO Roles
    (Id, RoleName)
VALUES (3, 'Manager');

INSERT INTO Roles
    (Id, RoleName)
VALUES (4, 'Admin');

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000001', 'Emily', 'Anderson', '1995-05-15', '123 Main Street, Vancouver, BC', 1,
        25.50);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000002', 'Michael', 'Brown', '1992-08-22', '456 Oak Avenue, Vancouver, BC', 1,
        24.75);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000003', 'Sarah', 'Chen', '1998-03-10', '789 Elm Drive, Vancouver, BC', 1,
        26.00);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000005', 'James', 'Wilson', '1994-11-30', '321 Pine Road, Vancouver, BC', 1,
        25.00);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000006', 'Jennifer', 'Taylor', '1996-07-18', '654 Maple Lane, Vancouver, BC', 1,
        24.50);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000007', 'David', 'Martinez', '1993-09-05', '987 Cedar Court, Vancouver, BC', 1,
        26.50);

INSERT INTO Users
    (UserId, FirstName, LastName, DateOfBirth, Address, RoleId, HourlyWage)
VALUES ('11111111-1111-1111-1111-000000000008', 'Jessica', 'Lee', '1997-12-14', '147 Birch Street, Vancouver, BC', 1,
        25.25);

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000001', 'D1 Morning', 3, '08:00:00', '16:00:00', '2026-01-01', '2026-12-31', 0,
        '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000002', 'D2 Day', 5, '09:00:00', '17:00:00', '2026-01-01', '2026-12-31', 0, '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000004', 'D4 Afternoon', 2, '12:00:00', '17:00:00', '2026-01-01', '2026-12-31',
        0, '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000005', 'D5 Mid-Morning', 2, '10:00:00', '13:30:00', '2026-01-01', '2026-12-31',
        0, '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000006', 'D6 Mid-Afternoon', 2, '13:30:00', '19:00:00', '2026-01-01',
        '2026-12-31', 0, '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000007', 'D7 Early', 3, '07:00:00', '15:00:00', '2026-01-01', '2026-12-31', 0,
        '');

INSERT INTO Shifts
(ShiftId, `Name`, RequiredCount, StartTime, EndTime, StartDate, EndDate, RecurrenceType, RecurrenceDays)
VALUES ('22222222-2222-2222-2222-000000000008', 'D8 Overlapping', 3, '10:00:00', '18:00:00', '2026-01-01', '2026-12-31',
        0, '');
