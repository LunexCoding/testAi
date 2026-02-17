select OrderApproval.*, OrderApprovalNomenclatureGroups.*, OrderApprovalTypes.* from OrderApproval
left join zayvka on zayvka.id = OrderApproval.zayvkaID
left join OrderApprovalNomenclatureGroups on OrderApprovalNomenclatureGroups.ID = OrderApproval.NomenclatureGroupID
left join OrderApprovalTypes on OrderApprovalTypes.ID = OrderApprovalNomenclatureGroups.TypeID
where zayvka.zak_1 = '23-5467'
