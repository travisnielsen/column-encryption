select * from sys.column_master_key_definitions
-- select * from sys.column_master_keys
select * from sys.column_encryption_keys
select * from sys.column_encryption_key_values

SELECT * FROM userdata
-- DELETE from userdata where PatientId > 5


-- USER INSERT TEST w/ parameters
DECLARE @firstname nvarchar(50) = 'Alice'
DECLARE @lastname nvarchar(50) = 'Kalle'
DECLARE @cc nvarchar(50) = '7523124545'
insert into dbo.userdata (first_name, last_name, cc) 
VALUES (@firstname, @lastname, @cc)
SELECT * FROM userdata