INSERT INTO OrderApprovalDrafts (
	OrderApprovalID,
	EquipmentDraft,
	EquipmentName,
	EquimentRequiredQuantity,
	CommentForDesign,
	CommentForManufacturing
)
SELECT DISTINCT
    main_oa.ID AS OrderApprovalID,
    z.draftosn AS EquipmentDraft,
	z.naimosn AS EquipmentName,
    z.kolzak AS EquimentRequiredQuantity,
    z.comments AS CommentForDesign,
    z.dop AS CommentForManufacturing
FROM zayvka z
-- Находим ОДИН основной OrderApproval для этого OrderNumber
INNER JOIN (
    -- Выбираем по одной записи OrderApproval для каждого zak_1
    SELECT DISTINCT
        FIRST_VALUE(oa.ID) OVER (PARTITION BY z_inner.zak_1 ORDER BY oa.ID) AS ID,
        z_inner.zak_1
    FROM OrderApproval oa
    INNER JOIN zayvka z_inner ON oa.zayvkaID = z_inner.id
) main_oa ON z.zak_1 = main_oa.zak_1
-- Фильтры из оригинального запроса
INNER JOIN os_pro op ON z.zak_1 = op.zak_1
WHERE op.d_vn_14 IS NOT NULL
