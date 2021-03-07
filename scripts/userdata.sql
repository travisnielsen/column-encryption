CREATE TABLE [dbo].[userdata](
         [ID] [int] IDENTITY(1,1),
         [FirstName] [nvarchar](50) NULL,
         [LastName] [nvarchar](50) NULL,
         [SSN] [nvarchar](12) NULL,
         [OtherTIN] [nvarchar](12) NULL,
         [Email] [nvarchar](50) NULL,
         [Gender] [nvarchar](10) NULL,
         [CreditRating] [smallint] NULL,
         [LastIPAddress] [nvarchar](50) NULL,
         [BirthDate] [date] NULL,
         [Salary] [smallmoney] NULL,
         [LastLocationLattitude][nvarchar](100) NULL,
         [LastLocationLongitude][nvarchar](100) NULL,
         PRIMARY KEY CLUSTERED ([ID] ASC) ON [PRIMARY] );
GO