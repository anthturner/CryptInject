CREATE TABLE [dbo].[Patients]
(
	[PatientId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [_encStored_FirstName] VARBINARY(MAX) NULL, 
    [_encStored_LastName] VARBINARY(MAX) NULL, 
    [_encStored_SSN] VARBINARY(MAX) NULL, 
    [_encStored_DOB] VARBINARY(MAX) NULL, 
    [_encStored_Glucose] VARBINARY(MAX) NULL, 
    [_encStored_CPeptide] VARBINARY(MAX) NULL, 
    [_encStored_ALT] VARBINARY(MAX) NULL, 
    [_encStored_AST] VARBINARY(MAX) NULL, 
    [_encStored_BMI] VARBINARY(MAX) NULL, 
    [_encStored_HDL] VARBINARY(MAX) NULL, 
    [Collected] DATETIME NULL
)
