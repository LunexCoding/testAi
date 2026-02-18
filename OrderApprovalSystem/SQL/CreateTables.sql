CREATE TABLE OrderApprovalTypes (
	ID INT PRIMARY KEY IDENTITY(1, 1),
	[Group] VARCHAR(150) NOT NULL,
	NewDevelopmentTerm INT NOT NULL,
	DoubleTerm INT NOT NULL,
	RepairModificationTerm INT NOT NULL,
	
	CONSTRAINT CHK_OrderApprovalTypes_Group
    CHECK ([Group] IN ('Группа 1', 'Группа 2', 'Группа 3', 'Группа 4', 'Группа 5', 'Группа 6', 'Группа 7', 'Группа 8'))
)


CREATE TABLE OrderApproval (
	ID INT PRIMARY KEY IDENTITY(1, 1),
	zayvkaID INT, 									-- zayvka.ID
	ManufacturingTerm DATE,					
	OpenAt DATE, 									-- zayvka.dza
	Draft NUMERIC(13, 2),					-- zayvka.draft
	DraftName VARCHAR(MAX),							-- LIST_FR / olistdse / listdse
	Technologist VARCHAR(100),						-- zayvka.imyt
	CoreDraft NUMERIC(13, 2),						-- zayvka.draftiz
	CoreDraftName VARCHAR(MAX),						-- zayvka.mdraftiz
	Workshop CHAR(3),								-- zayvka.cexpol
	Warehouse NUMERIC(3, 0),						-- zayvka.kladov
	Workplace NUMERIC(5, 0),						-- zayvka.rab_m
	Operation NUMERIC(4, 0),						-- zayvka.kodop
	EquipmentNameFromTechnologist VARCHAR(MAX),		-- zayvka.naosnsl
	Analog CHAR(20),								-- zayvka.prim_an
	EquipmentQuantityForOperation NUMERIC(4, 1),	-- zayvka.koltex	

    IsByMemo BIT NOT NULL DEFAULT 0,
    MemoNumber VARCHAR(255),
    MemoAuthor VARCHAR(255),
	[Order] INT,
    Number INT,
	OrderName VARCHAR(250),
	CoreOrder INT,
	CoreNumber INT,
	OpenAtByMemo Date,
    NomenclatureGroupID INT,
    EquipmentTypeID INT,                            -- OrderApprovalTypes.ID
    DraftByMemo NUMERIC(13, 2),
    DraftNameByMemo VARCHAR(MAX),
    Balance INT,
	WorkshopByMemo CHAR(3),
	EquipmentRequiredQuantityByMemo NUMERIC(6, 1),

	CONSTRAINT FK_OrderApproval_NomenclatureGroupID 
	FOREIGN KEY (NomenclatureGroupID) REFERENCES OrderApprovalNomenclatureGroups(ID),

	CONSTRAINT FK_OrderApproval_EquipmentTypeID 
	FOREIGN KEY (EquipmentTypeID) REFERENCES OrderApprovalTypes(ID)
)


CREATE TABLE OrderApprovalDrafts (
	ID INT PRIMARY KEY IDENTITY(1, 1),
	OrderApprovalID INT NOT NULL, 				-- OrderApproval.ID
	EquipmentDraft NUMERIC(13, 2) NOT NULL,		-- zayvka.draftosn
	EquipmentName VARCHAR(MAX),					-- zayvka.naimosn
	Cooperation BIT DEFAULT 0 NOT NULL,					
	IsDeletedFromOrder BIT DEFAULT 0 NOT NULL,			
	EquimentRequiredQuantity NUMERIC(6, 1),		-- zayvka.kolzak
	CommentForDesign VARCHAR(MAX),				-- zayvka.comments
	CommentForManufacturing VARCHAR(MAX),		-- zayvka.dop
	
	CONSTRAINT FK_OrderApprovalDrafts_OrderApprovalID 
	FOREIGN KEY (OrderApprovalID) REFERENCES OrderApproval(ID)
)


CREATE TABLE OrderApprovalHistory (
	ID INT PRIMARY KEY IDENTITY(1, 1),
	OrderApprovalID INT NOT NULL,
	ParentID INT NULL,					
	ReceiptDate DATETIME2(0) NOT NULL, 					--
	CompletionDate DATETIME2(0) NULL,					--
	Term DATE NOT NULL, 								-- ReceiptDate.Day + (OrderApproval.ID -> OrderApproval.EquipmentTypeID -> OrderApprovalTypes.Term)
	RecipientRole VARCHAR(100) NOT NULL,				--
	RecipientName VARCHAR(150) NOT NULL,				--
	SenderRole VARCHAR(100) NOT NULL,					--
	SenderName VARCHAR(150) NOT NULL,					--
	[Status] VARCHAR(9) DEFAULT 'В работе',	--
	Result VARCHAR(20) NULL,							
	IsRework BIT DEFAULT 0,
	Comment VARCHAR(MAX) NULL,							--
	
	CONSTRAINT FK_OrderApprovalHistory_OrderApprovalID 
	FOREIGN KEY (OrderApprovalID) REFERENCES OrderApproval(ID),

	CONSTRAINT CHK_OrderApprovalHistory_Result 
    CHECK (Result IN ('Согласовано', 'Не согласовано', 'Аннулировать', NULL)),

	CONSTRAINT CHK_OrderApprovalHistory_Status 
    CHECK (Status IN ('В работе', 'Выполнено')),

	CONSTRAINT FK_OrderApprovalHistory_Parent 
	FOREIGN KEY (ParentID) REFERENCES OrderApprovalHistory(ID)
)


CREATE TABLE OrderApprovalNomenclatureGroups (
	ID INT PRIMARY KEY IDENTITY(1, 1),
	GroupName VARCHAR(255) NOT NULL UNIQUE,
	TypeID INT NOT NULL,

	CONSTRAINT FK_OrderApprovalNomenclatureGroups_OrderApprovalTypes
	FOREIGN KEY (TypeID) REFERENCES OrderApprovalTypes(ID)
)
