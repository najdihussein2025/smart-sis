IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Campuses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Campuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(16) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailOutbox] (
        [Id] int NOT NULL IDENTITY,
        [ToEmail] nvarchar(256) NOT NULL,
        [Subject] nvarchar(256) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Attempts] int NOT NULL,
        [SentAt] datetime2 NULL,
        [LastError] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmailOutbox] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [GradeScales] (
        [Id] int NOT NULL IDENTITY,
        [Letter] nvarchar(4) NOT NULL,
        [StatusLabel] nvarchar(32) NOT NULL,
        [GpaPoints] decimal(10,2) NOT NULL,
        [MinPct] int NOT NULL,
        [MaxPct] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_GradeScales] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GradeScales_Range] CHECK ([MinPct] >= 0 AND [MaxPct] <= 100 AND [MinPct] <= [MaxPct])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PaymentTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [SupportCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SupportCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Terms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(64) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [EnrollStart] datetime2 NOT NULL,
        [EnrollEnd] datetime2 NOT NULL,
        [AddDropStart] datetime2 NOT NULL,
        [AddDropEnd] datetime2 NOT NULL,
        [IsCurrent] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Terms] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Terms_AddDropWindow] CHECK ([AddDropStart] < [AddDropEnd]),
        CONSTRAINT [CK_Terms_Dates] CHECK ([StartDate] < [EndDate]),
        CONSTRAINT [CK_Terms_EnrollWindow] CHECK ([EnrollStart] < [EnrollEnd])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [UniversityId] bigint NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(100) NOT NULL,
        [Role] nvarchar(32) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(32) NULL,
        [DateOfBirth] date NULL,
        [PhotoUrl] nvarchar(1024) NULL,
        [IsActive] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [CampusId] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Capacity] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Rooms_Capacity] CHECK ([Capacity] > 0),
        CONSTRAINT [FK_Rooms_Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [Campuses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(16) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Credits] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [DepartmentId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Courses_Credits] CHECK ([Credits] BETWEEN 0 AND 12),
        CONSTRAINT [FK_Courses_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Majors] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [DefaultMaxCredits] int NOT NULL DEFAULT 18,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Majors] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Majors_DefaultMaxCredits] CHECK ([DefaultMaxCredits] > 0),
        CONSTRAINT [FK_Majors_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Instructors] (
        [UserId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Instructors] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Instructors_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Instructors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [KnowledgeDocuments] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [FileUrl] nvarchar(1024) NOT NULL,
        [UploadedBy] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_KnowledgeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KnowledgeDocuments_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(1000) NOT NULL,
        [Link] nvarchar(512) NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nchar(64) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [CoursePrerequisites] (
        [CourseId] int NOT NULL,
        [PrerequisiteId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CoursePrerequisites] PRIMARY KEY ([CourseId], [PrerequisiteId]),
        CONSTRAINT [CK_CoursePrerequisites_NotSelf] CHECK ([CourseId] <> [PrerequisiteId]),
        CONSTRAINT [FK_CoursePrerequisites_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CoursePrerequisites_Courses_PrerequisiteId] FOREIGN KEY ([PrerequisiteId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [GradeComponents] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [TermId] int NOT NULL,
        [Name] nvarchar(64) NOT NULL,
        [MaxGrade] decimal(10,2) NOT NULL,
        [Weight] decimal(10,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_GradeComponents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GradeComponents_MaxGrade] CHECK ([MaxGrade] > 0),
        CONSTRAINT [CK_GradeComponents_Weight] CHECK ([Weight] > 0 AND [Weight] <= 100),
        CONSTRAINT [FK_GradeComponents_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GradeComponents_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [ChargeBatches] (
        [Id] int NOT NULL IDENTITY,
        [MajorId] int NOT NULL,
        [TermId] int NOT NULL,
        [PaymentTypeId] int NOT NULL,
        [Title] nvarchar(128) NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [DueDate] date NOT NULL,
        [CreatedBy] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChargeBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChargeBatches_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_ChargeBatches_Majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [Majors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChargeBatches_PaymentTypes_PaymentTypeId] FOREIGN KEY ([PaymentTypeId]) REFERENCES [PaymentTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChargeBatches_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChargeBatches_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [MajorCourses] (
        [MajorId] int NOT NULL,
        [CourseId] int NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MajorCourses] PRIMARY KEY ([MajorId], [CourseId]),
        CONSTRAINT [FK_MajorCourses_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MajorCourses_Majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [Majors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [UserId] int NOT NULL,
        [MajorId] int NOT NULL,
        [CampusId] int NOT NULL,
        [EnrollmentDate] date NOT NULL,
        [MaxCreditsOverride] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_Students_MaxCreditsOverride] CHECK ([MaxCreditsOverride] IS NULL OR [MaxCreditsOverride] > 0),
        CONSTRAINT [FK_Students_Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [Campuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Students_Majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [Majors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Sections] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [TermId] int NOT NULL,
        [SectionCode] nvarchar(8) NOT NULL,
        [InstructorId] int NULL,
        [Capacity] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Sections_Capacity] CHECK ([Capacity] > 0),
        CONSTRAINT [FK_Sections_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sections_Instructors_InstructorId] FOREIGN KEY ([InstructorId]) REFERENCES [Instructors] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sections_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [KnowledgeChunks] (
        [Id] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Embedding] varbinary(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_KnowledgeChunks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KnowledgeChunks_KnowledgeDocuments_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [KnowledgeDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Charges] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [BatchId] int NULL,
        [TermId] int NOT NULL,
        [PaymentTypeId] int NOT NULL,
        [Title] nvarchar(128) NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [DueDate] date NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Charges] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Charges_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Charges_ChargeBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ChargeBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Charges_PaymentTypes_PaymentTypeId] FOREIGN KEY ([PaymentTypeId]) REFERENCES [PaymentTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Charges_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Charges_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [ChatConversations] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [DeletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatConversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatConversations_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [SupportRequests] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [AssignedAdminId] int NULL,
        [ResolvedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SupportRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupportRequests_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupportRequests_SupportCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [SupportCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupportRequests_Users_AssignedAdminId] FOREIGN KEY ([AssignedAdminId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [TermResults] (
        [StudentId] int NOT NULL,
        [TermId] int NOT NULL,
        [TermGpa] decimal(10,2) NOT NULL,
        [CumulativeGpa] decimal(10,2) NOT NULL,
        [CreditsAttempted] int NOT NULL,
        [CreditsEarned] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TermResults] PRIMARY KEY ([StudentId], [TermId]),
        CONSTRAINT [FK_TermResults_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TermResults_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Announcements] (
        [Id] int NOT NULL IDENTITY,
        [AuthorId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Scope] nvarchar(32) NOT NULL,
        [MajorId] int NULL,
        [SectionId] int NULL,
        [PublishAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Announcements_MajorScope] CHECK (([Scope] = 'Major' AND [MajorId] IS NOT NULL) OR ([Scope] <> 'Major' AND [MajorId] IS NULL)),
        CONSTRAINT [CK_Announcements_SectionScope] CHECK (([Scope] = 'Section' AND [SectionId] IS NOT NULL) OR ([Scope] <> 'Section' AND [SectionId] IS NULL)),
        CONSTRAINT [FK_Announcements_Majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [Majors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Announcements_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Announcements_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SectionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CartItems_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [EnrollmentActions] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Action] nvarchar(32) NOT NULL,
        [FromSectionId] int NULL,
        [ToSectionId] int NULL,
        [PerformedBy] int NOT NULL,
        [PerformedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EnrollmentActions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EnrollmentActions_Sections_FromSectionId] FOREIGN KEY ([FromSectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EnrollmentActions_Sections_ToSectionId] FOREIGN KEY ([ToSectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EnrollmentActions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EnrollmentActions_Users_PerformedBy] FOREIGN KEY ([PerformedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SectionId] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [FinalPct] decimal(10,2) NULL,
        [LetterGrade] nvarchar(4) NULL,
        [GradePoints] decimal(10,2) NULL,
        [CountsInGpa] bit NOT NULL,
        [EnrolledAt] datetime2 NOT NULL,
        [DroppedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Enrollments_FinalPct] CHECK ([FinalPct] IS NULL OR ([FinalPct] >= 0 AND [FinalPct] <= 100)),
        CONSTRAINT [FK_Enrollments_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [SectionMeetings] (
        [Id] int NOT NULL IDENTITY,
        [SectionId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [RoomId] int NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SectionMeetings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SectionMeetings_Day] CHECK ([DayOfWeek] BETWEEN 1 AND 6),
        CONSTRAINT [CK_SectionMeetings_Time] CHECK ([StartTime] < [EndTime]),
        CONSTRAINT [FK_SectionMeetings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SectionMeetings_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [ChargeId] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [PaidAt] datetime2 NOT NULL,
        [RecordedBy] int NOT NULL,
        [Note] nvarchar(512) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Payments_Charges_ChargeId] FOREIGN KEY ([ChargeId]) REFERENCES [Charges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Users_RecordedBy] FOREIGN KEY ([RecordedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [Role] nvarchar(32) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_ChatConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [ChatConversations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [SupportMessages] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [AuthorId] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SupportMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupportMessages_SupportRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [SupportRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupportMessages_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [AbsenceAlerts] (
        [Id] int NOT NULL IDENTITY,
        [EnrollmentId] int NOT NULL,
        [Level] nvarchar(32) NOT NULL,
        [AbsenceCount] int NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AbsenceAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AbsenceAlerts_Enrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [Enrollments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentGrades] (
        [Id] int NOT NULL IDENTITY,
        [EnrollmentId] int NOT NULL,
        [ComponentId] int NOT NULL,
        [Score] decimal(10,2) NOT NULL,
        [GradedBy] int NOT NULL,
        [GradedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentGrades] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StudentGrades_Score] CHECK ([Score] >= 0),
        CONSTRAINT [FK_StudentGrades_Enrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [Enrollments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentGrades_GradeComponents_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [GradeComponents] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentGrades_Users_GradedBy] FOREIGN KEY ([GradedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendanceSessions] (
        [Id] int NOT NULL IDENTITY,
        [SectionId] int NOT NULL,
        [MeetingId] int NOT NULL,
        [SessionDate] date NOT NULL,
        [SubmittedBy] int NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceSessions_SectionMeetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [SectionMeetings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceSessions_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceSessions_Users_SubmittedBy] FOREIGN KEY ([SubmittedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendanceRecords] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [EnrollmentId] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceRecords_AttendanceSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AttendanceSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceRecords_Enrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [Enrollments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'GpaPoints', N'Letter', N'MaxPct', N'MinPct', N'StatusLabel', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[GradeScales]'))
        SET IDENTITY_INSERT [GradeScales] ON;
    EXEC(N'INSERT INTO [GradeScales] ([Id], [CreatedAt], [GpaPoints], [Letter], [MaxPct], [MinPct], [StatusLabel], [UpdatedAt])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', 4.0, N''A+'', 100, 97, N''Outstanding'', ''2026-01-01T00:00:00.0000000Z''),
    (2, ''2026-01-01T00:00:00.0000000Z'', 4.0, N''A'', 96, 93, N''Excellent'', ''2026-01-01T00:00:00.0000000Z''),
    (3, ''2026-01-01T00:00:00.0000000Z'', 3.7, N''A-'', 92, 89, N''Very Good'', ''2026-01-01T00:00:00.0000000Z''),
    (4, ''2026-01-01T00:00:00.0000000Z'', 3.3, N''B+'', 88, 85, N''Good'', ''2026-01-01T00:00:00.0000000Z''),
    (5, ''2026-01-01T00:00:00.0000000Z'', 3.0, N''B'', 84, 80, N''Good'', ''2026-01-01T00:00:00.0000000Z''),
    (6, ''2026-01-01T00:00:00.0000000Z'', 2.7, N''B-'', 79, 77, N''Good'', ''2026-01-01T00:00:00.0000000Z''),
    (7, ''2026-01-01T00:00:00.0000000Z'', 2.3, N''C+'', 76, 73, N''Satisfactory'', ''2026-01-01T00:00:00.0000000Z''),
    (8, ''2026-01-01T00:00:00.0000000Z'', 2.0, N''C'', 72, 70, N''Satisfactory'', ''2026-01-01T00:00:00.0000000Z''),
    (9, ''2026-01-01T00:00:00.0000000Z'', 1.7, N''C-'', 69, 66, N''Satisfactory'', ''2026-01-01T00:00:00.0000000Z''),
    (10, ''2026-01-01T00:00:00.0000000Z'', 1.3, N''D+'', 65, 63, N''Passing'', ''2026-01-01T00:00:00.0000000Z''),
    (11, ''2026-01-01T00:00:00.0000000Z'', 1.0, N''D'', 62, 60, N''Passing'', ''2026-01-01T00:00:00.0000000Z''),
    (12, ''2026-01-01T00:00:00.0000000Z'', 0.0, N''F'', 59, 0, N''Failure'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'GpaPoints', N'Letter', N'MaxPct', N'MinPct', N'StatusLabel', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[GradeScales]'))
        SET IDENTITY_INSERT [GradeScales] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AbsenceAlerts_EnrollmentId_Level] ON [AbsenceAlerts] ([EnrollmentId], [Level]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_AuthorId] ON [Announcements] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_MajorId] ON [Announcements] ([MajorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_Scope_PublishAt] ON [Announcements] ([Scope], [PublishAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_SectionId] ON [Announcements] ([SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AttendanceRecords_EnrollmentId_Status] ON [AttendanceRecords] ([EnrollmentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttendanceRecords_SessionId_EnrollmentId] ON [AttendanceRecords] ([SessionId], [EnrollmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttendanceSessions_MeetingId_SessionDate] ON [AttendanceSessions] ([MeetingId], [SessionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AttendanceSessions_SectionId] ON [AttendanceSessions] ([SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AttendanceSessions_SubmittedBy] ON [AttendanceSessions] ([SubmittedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Campuses_Name] ON [Campuses] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartItems_SectionId] ON [CartItems] ([SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartItems_StudentId_SectionId] ON [CartItems] ([StudentId], [SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChargeBatches_CreatedBy] ON [ChargeBatches] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChargeBatches_MajorId] ON [ChargeBatches] ([MajorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChargeBatches_PaymentTypeId] ON [ChargeBatches] ([PaymentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChargeBatches_TermId] ON [ChargeBatches] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Charges_BatchId] ON [Charges] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Charges_PaymentTypeId] ON [Charges] ([PaymentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Charges_StudentId_Status] ON [Charges] ([StudentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Charges_TermId] ON [Charges] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChatConversations_StudentId] ON [ChatConversations] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ConversationId_CreatedAt] ON [ChatMessages] ([ConversationId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CoursePrerequisites_PrerequisiteId] ON [CoursePrerequisites] ([PrerequisiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Courses_Code] ON [Courses] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_DepartmentId] ON [Courses] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailOutbox_Status_CreatedAt] ON [EmailOutbox] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EnrollmentActions_FromSectionId] ON [EnrollmentActions] ([FromSectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EnrollmentActions_PerformedBy] ON [EnrollmentActions] ([PerformedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EnrollmentActions_StudentId_PerformedAt] ON [EnrollmentActions] ([StudentId], [PerformedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EnrollmentActions_ToSectionId] ON [EnrollmentActions] ([ToSectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_SectionId_Status] ON [Enrollments] ([SectionId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_StudentId_Status] ON [Enrollments] ([StudentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GradeComponents_CourseId_TermId_Name] ON [GradeComponents] ([CourseId], [TermId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GradeComponents_TermId] ON [GradeComponents] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GradeScales_Letter] ON [GradeScales] ([Letter]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Instructors_DepartmentId] ON [Instructors] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_KnowledgeChunks_DocumentId] ON [KnowledgeChunks] ([DocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_KnowledgeDocuments_UploadedBy] ON [KnowledgeDocuments] ([UploadedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MajorCourses_CourseId] ON [MajorCourses] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Majors_DepartmentId_Name] ON [Majors] ([DepartmentId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_ReadAt] ON [Notifications] ([UserId], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_ChargeId] ON [Payments] ([ChargeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_RecordedBy] ON [Payments] ([RecordedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentTypes_Name] ON [PaymentTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_CampusId_Name] ON [Rooms] ([CampusId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SectionMeetings_RoomId_DayOfWeek] ON [SectionMeetings] ([RoomId], [DayOfWeek]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SectionMeetings_SectionId] ON [SectionMeetings] ([SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sections_CourseId_TermId_SectionCode] ON [Sections] ([CourseId], [TermId], [SectionCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sections_InstructorId] ON [Sections] ([InstructorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sections_TermId_InstructorId] ON [Sections] ([TermId], [InstructorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentGrades_ComponentId] ON [StudentGrades] ([ComponentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentGrades_EnrollmentId_ComponentId] ON [StudentGrades] ([EnrollmentId], [ComponentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentGrades_GradedBy] ON [StudentGrades] ([GradedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_CampusId] ON [Students] ([CampusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_MajorId] ON [Students] ([MajorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SupportCategories_Name] ON [SupportCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportMessages_AuthorId] ON [SupportMessages] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportMessages_RequestId] ON [SupportMessages] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportRequests_AssignedAdminId] ON [SupportRequests] ([AssignedAdminId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportRequests_CategoryId] ON [SupportRequests] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportRequests_Status_CreatedAt] ON [SupportRequests] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SupportRequests_StudentId] ON [SupportRequests] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TermResults_TermId] ON [TermResults] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Terms_IsCurrent] ON [Terms] ([IsCurrent]) WHERE [IsCurrent] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Terms_Name] ON [Terms] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UniversityId] ON [Users] ([UniversityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028105907_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028105907_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerAssessments] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [TermId] int NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerAssessments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CareerAssessments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerAssessments_Terms_TermId] FOREIGN KEY ([TermId]) REFERENCES [Terms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerPaths] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerPaths] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerQuestions] (
        [Id] int NOT NULL IDENTITY,
        [Text] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerQuestions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerTraits] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(60) NOT NULL,
        [Description] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerTraits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerRoadmapSteps] (
        [Id] int NOT NULL IDENTITY,
        [CareerPathId] int NOT NULL,
        [StepOrder] int NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [CourseId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerRoadmapSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CareerRoadmapSteps_Order] CHECK ([StepOrder] >= 1),
        CONSTRAINT [FK_CareerRoadmapSteps_CareerPaths_CareerPathId] FOREIGN KEY ([CareerPathId]) REFERENCES [CareerPaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerRoadmapSteps_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerQuestionOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [OptionOrder] int NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerQuestionOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CareerQuestionOptions_CareerQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [CareerQuestions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerAssessmentTraitScores] (
        [Id] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [TraitId] int NOT NULL,
        [Points] int NOT NULL,
        [MaxPoints] int NOT NULL,
        [Score] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerAssessmentTraitScores] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CareerAssessmentTraitScores_Score] CHECK ([Score] BETWEEN 0 AND 100),
        CONSTRAINT [FK_CareerAssessmentTraitScores_CareerAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [CareerAssessments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerAssessmentTraitScores_CareerTraits_TraitId] FOREIGN KEY ([TraitId]) REFERENCES [CareerTraits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerPathTraits] (
        [Id] int NOT NULL IDENTITY,
        [CareerPathId] int NOT NULL,
        [TraitId] int NOT NULL,
        [Weight] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerPathTraits] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CareerPathTraits_Weight] CHECK ([Weight] BETWEEN 1 AND 10),
        CONSTRAINT [FK_CareerPathTraits_CareerPaths_CareerPathId] FOREIGN KEY ([CareerPathId]) REFERENCES [CareerPaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerPathTraits_CareerTraits_TraitId] FOREIGN KEY ([TraitId]) REFERENCES [CareerTraits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CourseTraits] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [TraitId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CourseTraits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseTraits_CareerTraits_TraitId] FOREIGN KEY ([TraitId]) REFERENCES [CareerTraits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CourseTraits_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerAssessmentAnswers] (
        [Id] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [QuestionId] int NOT NULL,
        [OptionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerAssessmentAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CareerAssessmentAnswers_CareerAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [CareerAssessments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerAssessmentAnswers_CareerQuestionOptions_OptionId] FOREIGN KEY ([OptionId]) REFERENCES [CareerQuestionOptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerAssessmentAnswers_CareerQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [CareerQuestions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE TABLE [CareerOptionTraitPoints] (
        [Id] int NOT NULL IDENTITY,
        [OptionId] int NOT NULL,
        [TraitId] int NOT NULL,
        [Points] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CareerOptionTraitPoints] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CareerOptionTraitPoints_Points] CHECK ([Points] BETWEEN 1 AND 5),
        CONSTRAINT [FK_CareerOptionTraitPoints_CareerQuestionOptions_OptionId] FOREIGN KEY ([OptionId]) REFERENCES [CareerQuestionOptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CareerOptionTraitPoints_CareerTraits_TraitId] FOREIGN KEY ([TraitId]) REFERENCES [CareerTraits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerAssessmentAnswers_AssessmentId_QuestionId] ON [CareerAssessmentAnswers] ([AssessmentId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerAssessmentAnswers_OptionId] ON [CareerAssessmentAnswers] ([OptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerAssessmentAnswers_QuestionId] ON [CareerAssessmentAnswers] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerAssessments_StudentId_TermId] ON [CareerAssessments] ([StudentId], [TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerAssessments_TermId] ON [CareerAssessments] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerAssessmentTraitScores_AssessmentId_TraitId] ON [CareerAssessmentTraitScores] ([AssessmentId], [TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerAssessmentTraitScores_TraitId] ON [CareerAssessmentTraitScores] ([TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerOptionTraitPoints_OptionId_TraitId] ON [CareerOptionTraitPoints] ([OptionId], [TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerOptionTraitPoints_TraitId] ON [CareerOptionTraitPoints] ([TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerPaths_Name] ON [CareerPaths] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerPathTraits_CareerPathId_TraitId] ON [CareerPathTraits] ([CareerPathId], [TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerPathTraits_TraitId] ON [CareerPathTraits] ([TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerQuestionOptions_QuestionId_OptionOrder] ON [CareerQuestionOptions] ([QuestionId], [OptionOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerQuestions_IsActive] ON [CareerQuestions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerRoadmapSteps_CareerPathId_StepOrder] ON [CareerRoadmapSteps] ([CareerPathId], [StepOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CareerRoadmapSteps_CourseId] ON [CareerRoadmapSteps] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerTraits_Name] ON [CareerTraits] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourseTraits_CourseId_TraitId] ON [CourseTraits] ([CourseId], [TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    CREATE INDEX [IX_CourseTraits_TraitId] ON [CourseTraits] ([TraitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114202515_AddCareerModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114202515_AddCareerModule', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_SupportMessages_RequestId] ON [SupportMessages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_ChatConversations_StudentId] ON [ChatConversations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Charges_BatchId] ON [Charges];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_SupportMessages_RequestId_CreatedAt] ON [SupportMessages] ([RequestId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Notifications_Type_Link] ON [Notifications] ([Type], [Link]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_CreatedAt] ON [Notifications] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_ChatConversations_StudentId_UpdatedAt] ON [ChatConversations] ([StudentId], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Charges_BatchId_Status] ON [Charges] ([BatchId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Charges_Status_DueDate] ON [Charges] ([Status], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Announcements_PublishAt] ON [Announcements] ([PublishAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114223931_AddPerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114223931_AddPerformanceIndexes', N'10.0.15');
END;

COMMIT;
GO

