[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]$serverName,

    [Parameter(Mandatory)]
    [string]$database
)

$connectionString = "Server=tcp:${serverName}.database.windows.net,1433;Initial Catalog=${database};Persist Security Info=False;User ID=sqladmin;Password=${serverName}A!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$qry =@"
SELECT c.name as Name, c.key_store_provider_name as KeyProvider,c.key_path as KeyPath  from sys.column_master_key_definitions as c;
SELECT cek.name as Name, cekv.encrypted_value as EncryptedColumnKey, cekv.encryption_algorithm_name as Algorithm, cmk.name AS 'ColumnMasterKeyName' from sys.column_encryption_keys as cek LEFT JOIN sys.column_encryption_key_values as cekv on cek.column_encryption_key_id = cek.column_encryption_key_id LEFT JOIN sys.column_master_key_definitions as cmk ON cekv.column_master_key_definition_id = cmk.column_master_key_definition_id;
SELECT c.name AS ColumnName, k.name AS ColumnKeyName, c.encryption_type_desc AS EncryptionType, c.encryption_algorithm_name AS Algorithm FROM sys.columns c INNER JOIN sys.column_encryption_keys k ON c.column_encryption_key_id = k.column_encryption_key_id INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE encryption_type IS NOT NULL
"@

$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
$cmd = New-Object System.Data.SqlClient.SqlCommand($qry, $connection) 

$ds = New-Object System.Data.DataSet
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$adapter.Fill($ds) | Out-Null
$connection.Close()

$cmks = ($ds.Tables[0] | Select-Object $ds.Tables[0].Columns.ColumnName )
$ceks = ($ds.Tables[1] | Select-Object $ds.Tables[1].Columns.ColumnName )
$cols = ($ds.Tables[2] | Select-Object $ds.Tables[2].Columns.ColumnName )

# Need to convert byte array back to a hexidecimal string. There might be a way to prevent this when reading from the dataset
foreach ($item in $ceks) {
    $hexValue = ($item.EncryptedColumnKey | ForEach-Object ToString X2) -join ''
    $item.EncryptedColumnKey = "0x" + $hexValue
}

$colsObj = new-object -TypeName PSObject
$colsObj | Add-Member -MemberType NoteProperty -Name ColumnEncryptionInfo -Value $cols
$colsObj | Add-Member -MemberType NoteProperty -Name ColumnKeyInfo -Value $ceks
$colsObj | Add-Member -MemberType NoteProperty -Name ColumnMasterKeyInfo -Value $cmks

Import-Module powershell-yaml
ConvertTo-Yaml $colsObj -OutFile "${PSScriptRoot}\config.yaml"