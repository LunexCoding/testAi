-- создание по сз
select
	OrderApproval.ID,
	IsByMemo,
	MemoAuthor,
	[Order],
	Number,
	OrderName,
	CoreOrder,
	CoreNumber,
	OpenAtByMemo,
	NomenclatureGroup,
	OrderApprovalTypes.TypeName,
	DraftByMemo,
	DraftNameByMemo,
	Balance,
	WorkshopByMemo,
	EquipmentRequiredQuantityByMemo
from OrderApproval
left join OrderApprovalTypes on OrderApprovalTypes.ID = OrderApproval.EquipmentTypeID
where MemoNumber = 2

select * from OrderApprovalHistory where OrderApprovalID = 278
-- создание по сз

-- тех заказ
select
	OrderApproval.ID,
	zayvka.id as ZayvkaID,
	IsByMemo,
	MemoAuthor,
	[Order],
	Number,
	OrderName,
	CoreOrder,
	CoreNumber,
	OpenAtByMemo,
	NomenclatureGroup,
	OrderApprovalTypes.TypeName,
	DraftByMemo,
	DraftNameByMemo,
	Balance,
	WorkshopByMemo,
	EquipmentRequiredQuantityByMemo
from OrderApproval
left join OrderApprovalTypes on OrderApprovalTypes.ID = OrderApproval.EquipmentTypeID
left join zayvka on zayvka.ID = OrderApproval.zayvkaID
where zayvka.zak_1 = '23-5467'

select OrderApprovalHistory.* from OrderApprovalHistory
left join OrderApproval on OrderApproval.ID = OrderApprovalHistory.OrderApprovalID
left join zayvka on zayvka.ID = OrderApproval.zayvkaID
where zayvka.zak_1 = '23-5467'
-- тех заказ