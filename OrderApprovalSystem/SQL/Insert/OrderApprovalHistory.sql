-- Вставка данных в OrderApprovalHistory с учетом рабочих дней
INSERT INTO OrderApprovalHistory (
    OrderApprovalID,
    ReceiptDate,
    CompletionDate,
    Term,
    RecipientRole,
    RecipientName,
    SenderRole,
    SenderName,
    Result,
    Comment
)
SELECT 
    oa.ID AS OrderApprovalID,
    CAST(GETDATE() AS DATETIME2(0)) AS ReceiptDate,
    NULL AS CompletionDate,
    -- Term: ReceiptDate + RepairModificationTerm рабочих дней из OrderApprovalTypes
    (SELECT MIN(mday) 
     FROM (
         SELECT mday, 
                ROW_NUMBER() OVER (ORDER BY mday) AS rn
         FROM calend 
         WHERE mday > CAST(GETDATE() AS DATE) 
           AND v = 1 -- только рабочие дни
     ) AS work_days
     WHERE rn = oat.RepairModificationTerm
    ) AS Term,
    'Технолог' AS RecipientRole,
    'Рагульский' AS RecipientName,
    'ПДО' AS SenderRole,
    'Сергеев' AS SenderName,
    NULL AS Result,
    'Срок рассчитан по RepairModificationTerm' AS Comment
FROM OrderApproval oa
INNER JOIN OrderApprovalTypes oat ON oa.EquipmentTypeID = oat.ID
WHERE oa.EquipmentTypeID IS NOT NULL;
