INSERT INTO Roles (Id, RoleName)
SELECT 1, 'Employee'
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Employee');

INSERT INTO Roles (Id, RoleName)
SELECT 2, 'HR'
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'HR');

INSERT INTO Roles (Id, RoleName)
SELECT 3, 'Manager'
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Manager');

INSERT INTO Roles (Id, RoleName)
SELECT 4, 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin');
