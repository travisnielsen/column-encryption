-- Column Master Key (CMK) data
select c.name as Name, c.key_store_provider_name as KeyProvider,c.key_path as KeyPath  from sys.column_master_key_definitions as c
-- select * from sys.column_master_keys

-- Column Encryption Key (CEK) data
select cek.name as Name, cekv.encrypted_value as EncryptedColumnKey, cekv.encryption_algorithm_name as Algorithm, cmk.name AS 'ColumnMasterKeyName' from sys.column_encryption_keys as cek LEFT JOIN sys.column_encryption_key_values as cekv on cek.column_encryption_key_id = cek.column_encryption_key_id LEFT JOIN sys.column_master_key_definitions as cmk ON cekv.column_master_key_definition_id = cmk.column_master_key_definition_id

-- Column data
SELECT c.name AS ColumnName, k.name AS KeyName, c.encryption_type_desc AS EncryptionType, c.encryption_algorithm_name AS Algorithm FROM sys.columns c INNER JOIN sys.column_encryption_keys k ON c.column_encryption_key_id = k.column_encryption_key_id INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE encryption_type IS NOT NULL


-- DELETE from userdata where ID > 0

-- USER INSERT TEST w/ parameters
DECLARE @firstname nvarchar(50) = 'Alice'
DECLARE @lastname nvarchar(50) = 'Kalle'
DECLARE @cc nvarchar(50) = '7523124545'
insert into dbo.userdata (first_name, last_name, cc) 
VALUES (@firstname, @lastname, @cc)
SELECT * FROM userdata