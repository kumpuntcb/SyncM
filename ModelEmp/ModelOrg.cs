using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEmpMEA.ModelOrg
{

    public class Meta
    {
        public string Copyright { get; set; }
        public List<string> Authors { get; set; }
    }
    public class Root
    {
        public Meta Meta { get; set; }
        public List<OrganizationModel> Data { get; set; }
    }
    public class OrganizationModel
    {
        public int OrgSyEmpId { get; set; } 
        public int? OrgId { get; set; }
        public int? ParentOrgId { get; set; }
        public string OrgLevel { get; set; }
        public string Name { get; set; }
        public string NameSht { get; set; }
        public int? PathId { get; set; }
        public string PathNameSht { get; set; }
        public int? AssistId { get; set; }
        public string AssistNameSht { get; set; }
        public int? DepId { get; set; }
        public string DepNameSht { get; set; }
        public int? DivId { get; set; }
        public string DivNameSht { get; set; }
        public int? SecId { get; set; }
        public string SecNameSht { get; set; }
        public int? PartId { get; set; }
        public string PartNameSht { get; set; }
        public string CostCenter { get; set; }
    }
}
