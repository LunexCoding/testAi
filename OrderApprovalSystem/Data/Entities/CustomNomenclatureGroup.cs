using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderApprovalSystem.Data.Entities
{
    public class CustomNomenclatureGroup
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public OrderApprovalTypes Type { get; set; }

        public string TypeGroup => Type?.Group;
        public int NewDevelopmentTerm => Type?.NewDevelopmentTerm ?? 0;
        public int DoubleTerm => Type?.DoubleTerm ?? 0;
        public int RepairModificationTerm => Type?.RepairModificationTerm ?? 0;
        public string GroupNameWithGroup => $"{GroupName} ({TypeGroup})";
        
        // Получить значение термина по типу оснастки
        public int GetTermValue(string equipmentType)
        {
            switch (equipmentType?.ToLower())
            {
                case "новая разработка":
                    return NewDevelopmentTerm;
                case "дублер":
                    return DoubleTerm;
                case "ремонт/доработка":
                    return RepairModificationTerm;
                default:
                    return 0;
            }
        }
    }
}