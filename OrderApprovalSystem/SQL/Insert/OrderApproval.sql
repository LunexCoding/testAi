-- 1. Заполнение OrderApproval с номенклатурной группой и её типом
WITH DraftData AS (
    SELECT DISTINCT
        oa.zayvkaID,
        NULL AS ManufacturingTerm,
        z.dza AS OpenAt,
        z.draft AS Draft,
        z.imyt AS Technologist,
        z.draftiz AS CoreDraft,
        z.ndraftiz AS CoreDraftName,
        z.cexpol AS Workshop,
        z.kladov AS Warehouse,
        z.rab_m AS Workplace,
        z.kodop AS Operation,
        z.naosnsl AS EquipmentNameFromTechnologist,
        z.prim_an AS Analog,
        z.koltex AS EquipmentQuantityForOperation
    FROM OrderApprovalOLD oa
    INNER JOIN zayvka z ON oa.zayvkaID = z.id
    INNER JOIN os_pro op ON z.zak_1 = op.zak_1
    LEFT JOIN oborud ob ON z.rab_m = ob.rab_m
    LEFT JOIN s_oper so ON z.kodop = so.code
    LEFT JOIN prod p ON z.zak_1 = p.zak_1 AND p.dr IS NULL
    WHERE op.d_vn_14 IS NOT NULL
),
DraftDataWithNames AS (
    SELECT 
        dd.*,
        COALESCE(
            lf.NM,
            od.dse,
            ld.DSE,
            'Неизвестный узел'
        ) AS DraftName
    FROM DraftData dd
    LEFT JOIN LIST_FR lf ON lf.WHAT = dd.Draft
    LEFT JOIN olistdse od ON od.draft = dd.Draft
    LEFT JOIN listdse ld ON ld.DRAFT = FLOOR(dd.Draft / 1000.0)
),
RandomNomenclatureGroups AS (
    SELECT 
        ddwn.*,
        ng.ID AS NomenclatureGroupID,
        ng.TypeID AS EquipmentTypeID,
        ROW_NUMBER() OVER (PARTITION BY ddwn.zayvkaID ORDER BY NEWID()) AS rn
    FROM DraftDataWithNames ddwn
    CROSS JOIN OrderApprovalNomenclatureGroups ng
)
INSERT INTO OrderApproval (
    zayvkaID,
    ManufacturingTerm,
    OpenAt,
    Draft,
    DraftName,
    Technologist,
    CoreDraft,
    CoreDraftName,
    Workshop,
    Warehouse,
    Workplace,
    Operation,
    EquipmentNameFromTechnologist,
    Analog,
    EquipmentQuantityForOperation,
    NomenclatureGroupID,
    EquipmentTypeID
)
SELECT DISTINCT
    zayvkaID,
    ManufacturingTerm,
    OpenAt,
    Draft,
    DraftName,
    Technologist,
    CoreDraft,
    CoreDraftName,
    Workshop,
    Warehouse,
    Workplace,
    Operation,
    EquipmentNameFromTechnologist,
    Analog,
    EquipmentQuantityForOperation,
    NomenclatureGroupID,
    EquipmentTypeID
FROM RandomNomenclatureGroups
WHERE rn = 1
ORDER BY zayvkaID;
