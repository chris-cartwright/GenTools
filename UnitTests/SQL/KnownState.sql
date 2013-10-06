CREATE TYPE [dbo].[TableType] AS TABLE(
	[Column1] [int] NULL,
	[Column2] [nvarchar](max) NULL
)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE procedure [dbo].[p_Completely_Valid]
  @Column int
, @Second tinyint = 0
, @Third nvarchar(10)
, @Nullable int = null
, @Default nvarchar(50) = 'test default'
, @Output int = null output
, @DefString varchar(10) = ''
as

select 1



GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


create procedure [dbo].[p_MissingUnderscore]
  @Column int
as

select 1


GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


create procedure [dbo].[p_No_Params]
as

select 1


GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


create procedure [dbo].[p_Output_NonNull]
  @Tester smallint output,
  @String nvarchar(100) output
as

set @Tester = 5
set @String = 'Blarggy'


GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE procedure [dbo].[p_Output_Test]
  @Output int = null output,
  @String nvarchar(max) = null output
as

set @Output = 42
set @String = 'Marvin'


GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create procedure [dbo].[p_TableParameter]
	@Data TableType readonly
as

insert into NoIdentityType (NoIdentityID, NoIdentity)
select Column1, Column2 from @Data

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BadColumnType](
	[BadColumnID] [int] IDENTITY(1,1) NOT NULL,
	[DoesntExist] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_BadColumnType] PRIMARY KEY CLUSTERED 
(
	[BadColumnID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DuplicateType](
	[DuplicateID] [tinyint] IDENTITY(1,1) NOT NULL,
	[Duplicate] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_DuplicateType] PRIMARY KEY CLUSTERED 
(
	[DuplicateID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmptyType](
	[EmptyID] [int] IDENTITY(1,1) NOT NULL,
	[Empty] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_EmptyType] PRIMARY KEY CLUSTERED 
(
	[EmptyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[IllegalValueType](
	[IllegalID] [tinyint] IDENTITY(1,1) NOT NULL,
	[Illegal] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_IllegalValueType] PRIMARY KEY CLUSTERED 
(
	[IllegalID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NoIdentityType](
	[NoIdentityID] [int] NOT NULL,
	[NoIdentity] [nvarchar](50) NOT NULL
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NullableTypes](
	[NullableID] [smallint] IDENTITY(1,1) NOT NULL,
	[Nullable] [nvarchar](50) NULL,
 CONSTRAINT [PK_NullableTypes] PRIMARY KEY CLUSTERED 
(
	[NullableID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShouldntSee](
	[ShouldntSeeID] [bigint] IDENTITY(1,1) NOT NULL,
	[ShouldntSee] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ShouldntSee] PRIMARY KEY CLUSTERED 
(
	[ShouldntSeeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SingleColumnType](
	[SingleColumnID] [tinyint] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_SingleColumnIdentity] PRIMARY KEY CLUSTERED 
(
	[SingleColumnID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StandardTypes](
	[StandardID] [smallint] IDENTITY(1,1) NOT NULL,
	[Standard] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_StandardTypes] PRIMARY KEY CLUSTERED 
(
	[StandardID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TinyType](
	[TinyID] [tinyint] IDENTITY(1,1) NOT NULL,
	[Tiny] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TinyType] PRIMARY KEY CLUSTERED 
(
	[TinyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UniqueValues](
	[UniqueID] [tinyint] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_UniqueValues] PRIMARY KEY CLUSTERED 
(
	[UniqueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WrongType](
	[WrongTypeID] [smallint] IDENTITY(1,1) NOT NULL,
	[WrongType] [int] NOT NULL,
 CONSTRAINT [PK_WrongType] PRIMARY KEY CLUSTERED 
(
	[WrongTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET IDENTITY_INSERT [dbo].[BadColumnType] ON 

GO
INSERT [dbo].[BadColumnType] ([BadColumnID], [DoesntExist]) VALUES (1, N'Test')
GO
INSERT [dbo].[BadColumnType] ([BadColumnID], [DoesntExist]) VALUES (2, N'Test 2')
GO
INSERT [dbo].[BadColumnType] ([BadColumnID], [DoesntExist]) VALUES (3, N'Test 3')
GO
SET IDENTITY_INSERT [dbo].[BadColumnType] OFF
GO
SET IDENTITY_INSERT [dbo].[DuplicateType] ON 

GO
INSERT [dbo].[DuplicateType] ([DuplicateID], [Duplicate]) VALUES (1, N'Duplicate')
GO
INSERT [dbo].[DuplicateType] ([DuplicateID], [Duplicate]) VALUES (2, N'Duplicate')
GO
INSERT [dbo].[DuplicateType] ([DuplicateID], [Duplicate]) VALUES (3, N'No Duplicate')
GO
SET IDENTITY_INSERT [dbo].[DuplicateType] OFF
GO
SET IDENTITY_INSERT [dbo].[IllegalValueType] ON 

GO
INSERT [dbo].[IllegalValueType] ([IllegalID], [Illegal]) VALUES (1, N'Legal')
GO
INSERT [dbo].[IllegalValueType] ([IllegalID], [Illegal]) VALUES (2, N'Illegal !@#$%^&*()_+-=,.<>?/"''::|\}]{[~`')
GO
INSERT [dbo].[IllegalValueType] ([IllegalID], [Illegal]) VALUES (3, N'Bl@rg %')
GO
SET IDENTITY_INSERT [dbo].[IllegalValueType] OFF
GO
INSERT [dbo].[NoIdentityType] ([NoIdentityID], [NoIdentity]) VALUES (1, N'No ID')
GO
INSERT [dbo].[NoIdentityType] ([NoIdentityID], [NoIdentity]) VALUES (2, N'No ID2')
GO
INSERT [dbo].[NoIdentityType] ([NoIdentityID], [NoIdentity]) VALUES (3, N'No ID3')
GO
SET IDENTITY_INSERT [dbo].[NullableTypes] ON 

GO
INSERT [dbo].[NullableTypes] ([NullableID], [Nullable]) VALUES (1, N'Nullable')
GO
INSERT [dbo].[NullableTypes] ([NullableID], [Nullable]) VALUES (2, NULL)
GO
INSERT [dbo].[NullableTypes] ([NullableID], [Nullable]) VALUES (3, N'Nullable 3')
GO
SET IDENTITY_INSERT [dbo].[NullableTypes] OFF
GO
SET IDENTITY_INSERT [dbo].[ShouldntSee] ON 

GO
INSERT [dbo].[ShouldntSee] ([ShouldntSeeID], [ShouldntSee]) VALUES (1, N'Shouldn''t See')
GO
INSERT [dbo].[ShouldntSee] ([ShouldntSeeID], [ShouldntSee]) VALUES (2, N'Shouldn''t See 2')
GO
INSERT [dbo].[ShouldntSee] ([ShouldntSeeID], [ShouldntSee]) VALUES (3, N'Shouldn''t See 3')
GO
SET IDENTITY_INSERT [dbo].[ShouldntSee] OFF
GO
SET IDENTITY_INSERT [dbo].[SingleColumnType] ON 

GO
INSERT [dbo].[SingleColumnType] ([SingleColumnID]) VALUES (1)
GO
INSERT [dbo].[SingleColumnType] ([SingleColumnID]) VALUES (2)
GO
INSERT [dbo].[SingleColumnType] ([SingleColumnID]) VALUES (3)
GO
INSERT [dbo].[SingleColumnType] ([SingleColumnID]) VALUES (4)
GO
INSERT [dbo].[SingleColumnType] ([SingleColumnID]) VALUES (5)
GO
SET IDENTITY_INSERT [dbo].[SingleColumnType] OFF
GO
SET IDENTITY_INSERT [dbo].[StandardTypes] ON 

GO
INSERT [dbo].[StandardTypes] ([StandardID], [Standard]) VALUES (1, N'Standard')
GO
INSERT [dbo].[StandardTypes] ([StandardID], [Standard]) VALUES (2, N'Standard 2')
GO
INSERT [dbo].[StandardTypes] ([StandardID], [Standard]) VALUES (3, N'Standard 3')
GO
INSERT [dbo].[StandardTypes] ([StandardID], [Standard]) VALUES (4, N'NoSpace')
GO
INSERT [dbo].[StandardTypes] ([StandardID], [Standard]) VALUES (5, N'With Space')
GO
SET IDENTITY_INSERT [dbo].[StandardTypes] OFF
GO
SET IDENTITY_INSERT [dbo].[TinyType] ON 

GO
INSERT [dbo].[TinyType] ([TinyID], [Tiny]) VALUES (1, N'Tiny 1')
GO
INSERT [dbo].[TinyType] ([TinyID], [Tiny]) VALUES (2, N'Tiny 2')
GO
INSERT [dbo].[TinyType] ([TinyID], [Tiny]) VALUES (3, N'Tiny 3')
GO
SET IDENTITY_INSERT [dbo].[TinyType] OFF
GO
SET IDENTITY_INSERT [dbo].[WrongType] ON 

GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (1, 1)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (2, 4)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (3, 6)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (4, 3)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (5, 6)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (6, 2)
GO
INSERT [dbo].[WrongType] ([WrongTypeID], [WrongType]) VALUES (7, 76)
GO
SET IDENTITY_INSERT [dbo].[WrongType] OFF
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UniqueValues] ON [dbo].[UniqueValues]
(
	[Value] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
