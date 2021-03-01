CREATE TABLE [dbo].[userdata](
         [id] [int] IDENTITY(1,1),
         [first_name] [nvarchar](50) NULL,
         [last_name] [nvarchar](50) NULL,
         [email] [nvarchar](50) NULL,
         [gender] [nvarchar](10) NULL,
         [ip_address] [nvarchar](50) NULL,
         [cc] [nvarchar](50) NULL,
         [country] [nvarchar](50) NULL,
         [birthdate] [date] NULL,
         [salary] [smallmoney] NULL,
         [title] [nvarchar](50) NULL,
         PRIMARY KEY CLUSTERED ([id] ASC) ON [PRIMARY] );
GO