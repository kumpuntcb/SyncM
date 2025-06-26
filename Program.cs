using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using System.Data.SqlClient;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using WolfApprove.API2.Extension;

namespace SyncEmpMEA
{
    public static class Program
    {
        static string dbWolfConnectionString = ConfigurationManager.AppSettings["dbConnectionString"];
        static string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        static string ApiUrl = ConfigurationManager.AppSettings["ApiURL"];
        static string ApiOrgUrl = ConfigurationManager.AppSettings["ApiOrgURL"];
        static void Main(string[] args)
        {
            SyncDepDivPos();
            WriteLogFile("Start  SyncEmp " + DateTime.Now);
            SyncEmp();
            WriteLogFile("End SyncEmp " + DateTime.Now);
        }
        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<SyncEmpMEA.ModelEmp.AssistData, SyncEmpMEA.SyncEmp>();
            }
        }
        public class MappingProfileOrg : Profile
        {
            public MappingProfileOrg()
            {
                CreateMap<SyncEmpMEA.ModelOrg.OrganizationModel, OrganizationSyncEmp>();
            }
        }
        public static void SyncEmp()
        {
            try
            {
                WriteLogFile("1");
                DataWolfDataContext DbWolf = new DataWolfDataContext(dbWolfConnectionString);
                WriteLogFile("2");
                var datalist = DbWolf.SyncEmps.ToList();
                WriteLogFile("3");
                if (datalist.Any())
                {
                    bool result = BackupSyncEmpToBackDataSyncEmp(dbWolfConnectionString);
                    if (result)
                    {
                        DbWolf.ExecuteCommand("TRUNCATE TABLE [dbo].[SyncEmp]");
                        DbWolf.Dispose();
                        DbWolf = new DataWolfDataContext(dbWolfConnectionString);
                    }

                }
                string apiUrl = ApiUrl;
                string apiKey = ApiKey;
                string resultdata = string.Empty;
                HttpResponseMessage response = new HttpResponseMessage();
                HttpResponseMessage responseOrg = new HttpResponseMessage();
                string resultdataOrg = string.Empty;
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        WriteLogFile("4: Preparing request...");

                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var jsonBody = @"{
""outFields"": ""assistId,assistName,assistNameEng,assistShortName,assistShortNameEng,cLevel,costCenter,depId,depName,depNameEng,depShortName,depShortNameEng,divId,divName,divNameEng,divShortName,divShortNameEng,email,empId,empPicture,firstName,firstNameEng,isCommander,isContractEmployee,isNormalPeriod,jobId,jobName,jobNameEng,jobShortName,jobShortNameEng,lastName,lastNameEng,mcJobName,orgDisplayName,orgId,orgLabel,orgLevelId,orgName,orgNameEng,orgShortName,orgShortNameEng,partId,partName,partNameEng,partShortName,partShortNameEng,pathId,pathName,pathNameEng,pathShortName,pathShortNameEng,posId,posName,posNameEng,posShortName,posShortNameEng,prefix,prefixEng,secId,secName,secNameEng,secShortName,secShortNameEng,tel,telExtension,telInternalPrefix,telInternalSuffix,telOfficial,parrentid,empPriority,uuid""}";

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                        WriteLogFile("5: Sending POST request...");

                        response = client.PostAsync(apiUrl, content).GetAwaiter().GetResult();

                        WriteLogFile("6: Received response");

                        resultdata = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (response.IsSuccessStatusCode)
                        {
                            WriteLogFile("Success: " + resultdata);
                        }
                        else
                        {
                            WriteLogFile($"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");
                            WriteLogFile("Error content: " + resultdata);
                        }




                    }
                    catch (HttpRequestException ex)
                    {
                        WriteLogFile("HttpRequestException: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        WriteLogFile("Unhandled Exception: " + ex.Message);
                    }
                }
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfile>();
                });

                IMapper mapper = config.CreateMapper();
                ModelEmp.Root data = JsonConvert.DeserializeObject<ModelEmp.Root>(resultdata);

                WriteLogFile("EmpdataCount " + data.Data.Count());
                foreach (var insertdata in data.Data)
                {
                    WriteLogFile("Empdata " + insertdata.Uuid + " " + insertdata.Email + " " + insertdata.PosId);

                    SyncEmp indata = mapper.Map<SyncEmp>(insertdata);
                    indata.modifiledate = DateTime.Now;
                    DbWolf.SyncEmps.InsertOnSubmit(indata);
                    DbWolf.SubmitChanges();
                }
                var resultvalue = InsertEmpToWolf(DbWolf);

                using (HttpClient client2 = new HttpClient())
                {
                    try
                    {
                        string apiOrgUrl = ApiOrgUrl;
                        client2.DefaultRequestHeaders.Clear();
                        client2.DefaultRequestHeaders.Add("x-api-key", apiKey);
                        client2.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        WriteLogFile("5: Sending POST request...");

                        responseOrg = client2.GetAsync(apiOrgUrl).GetAwaiter().GetResult();

                        WriteLogFile("6: Received response");

                        resultdataOrg = responseOrg.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (responseOrg.IsSuccessStatusCode)
                        {
                            WriteLogFile("Success: " + responseOrg);
                        }
                        else
                        {
                            WriteLogFile($"HTTP {(int)responseOrg.StatusCode} - {responseOrg.ReasonPhrase}");
                            WriteLogFile("Error content: " + resultdataOrg);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        WriteLogFile("HttpRequestException: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        WriteLogFile("Unhandled Exception: " + ex.Message);
                    }
                }

                var config2 = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfileOrg>();

                });
                WriteLogFile("mapper2 ");
                IMapper mapper2 = config2.CreateMapper();
                WriteLogFile("mapper22 ");
                ModelOrg.Root data2 = JsonConvert.DeserializeObject<ModelOrg.Root>(resultdataOrg);
                WriteLogFile("mapper222 ");
                WriteLogFile("OrgCount " + data2.Data.Count());
                foreach (var insertdata in data2.Data)
                {
                    WriteLogFile("OrgData " + insertdata.OrgId + " " + insertdata.OrgLevel + " " + insertdata.Name);

                    OrganizationSyncEmp indata = mapper2.Map<OrganizationSyncEmp>(insertdata);
                    indata.ModifileDate = DateTime.Now;
                    DbWolf.OrganizationSyncEmps.InsertOnSubmit(indata);
                    DbWolf.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                Ext.ErrorLog(ex, "Error SyncEmp");
                WriteLogFile("Error SyncEmp : " + ex.Message);

            }
        }
        public static bool InsertEmpToWolf(DataWolfDataContext DbWolf)
        {
            try
            {
                var getdata = DbWolf.SyncEmps.ToList();
                foreach (var data in getdata)
                {
                    WriteLogFile("Emp data InsertEmpToWolf " + data.empId);
                    var getclevel = int.Parse(data.cLevel);
                    var checkEmp = DbWolf.MSTEmployees.Where(x => x.EmployeeCode == data.empId).FirstOrDefault();
                    var getdiv = DbWolf.MSTDivisions.Where(x => x.NameTh == data.pathName).FirstOrDefault();
                    var getdPos = DbWolf.MSTPositions.Where(x => x.NameTh == data.jobName).FirstOrDefault();
                    var getDep = DbWolf.MSTDepartments.Where(x => x.NameTh == data.orgName).FirstOrDefault();
                    var getEmpidReportTo = new MSTEmployee();
                    if (checkEmp != null)
                    {
                        WriteLogFile("--------------------------------------------------------------------------------");
                        WriteLogFile("Update Emp " + data.empId);
                        SyncEmp getReportTo = new SyncEmp();

                        if (getclevel <= 7)
                        {
                            getReportTo = getdata.Where(x => x.secId == data.secId && x.cLevel == "8").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        else if (getclevel == 8)
                        {
                            getReportTo = getdata.Where(x => x.divId == data.divId && x.cLevel == "9").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        else if (getclevel == 9)
                        {
                            getReportTo = getdata.Where(x => x.divId == data.divId && x.cLevel == "10").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        else if (getclevel == 10)
                        {
                            getReportTo = getdata.Where(x => x.depId == data.depId && x.cLevel == "11").OrderBy(x => x.emppriority).FirstOrDefault();

                        }
                        else if (getclevel == 11)
                        {
                            getReportTo = getdata.Where(x => x.pathId == data.pathId && x.cLevel == "12").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        else if (getclevel == 12)
                        {
                            getReportTo = getdata.Where(x => x.pathId == data.pathId && x.cLevel == "13").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        else if (getclevel == 13)
                        {
                            getReportTo = getdata.Where(x => x.cLevel == "21").OrderBy(x => x.emppriority).FirstOrDefault();
                        }
                        if (getReportTo != null)
                        {
                            getEmpidReportTo = DbWolf.MSTEmployees.Where(x => x.EmployeeCode == getReportTo.empId).FirstOrDefault();
                        }

                        checkEmp.Username = data.email;
                        checkEmp.NameTh = data.prefix + " " + data.firstName + " " + data.lastName;
                        checkEmp.Email = data.email;
                        checkEmp.NameEn = data.prefixEng + " " + data.firstNameEng + " " + data.lastNameEng;
                        checkEmp.PositionId = (getdPos != null) ? (int?)getdPos.PositionId : null;
                        checkEmp.DepartmentId = (getDep != null) ? (int?)getDep.DepartmentId : null;
                        checkEmp.ReportToEmpCode = getEmpidReportTo != null ? getEmpidReportTo.EmployeeId.ToString() : null;
                        checkEmp.ModifiedDate = DateTime.Now;
                        checkEmp.ModifiedBy = "System";
                        checkEmp.DivisionId = (getdiv != null && getdiv.DivisionId != 0) ? (int?)getdiv.DivisionId : null;
                        checkEmp.EmpLevel = getclevel.ToString();
                        DbWolf.SubmitChanges();
                        WriteLogFile("EmpName" + checkEmp.NameEn +" Email " + checkEmp.Email + " ReportToEmpCode " + checkEmp.ReportToEmpCode + " PositionId " + checkEmp .PositionId+ " DepartmentId " + checkEmp .DepartmentId+ " DivisionId "+ checkEmp.DivisionId);
                        WriteLogFile("--------------------------------------------------------------------------------");
                    }
                    else
                    {
                        WriteLogFile("--------------------------------------------------------------------------------");
                        WriteLogFile("Insert Emp " + data.empId);
                        MSTEmployee mSTEmployee = new MSTEmployee()
                        {
                            EmployeeCode = data.empId,
                            Username = data.email,
                            NameTh = data.prefix + " " + data.firstName + " " + data.lastName,
                            NameEn = data.prefixEng + " " + data.firstNameEng + " " + data.lastNameEng,
                            Email = data.email,
                            IsActive = true,
                            PositionId = (getdPos != null && getdPos.PositionId != 0) ? (int?)getdPos.PositionId : null,
                            DepartmentId = (getDep != null && getDep.DepartmentId != 0) ? (int?)getDep.DepartmentId : null,
                            ReportToEmpCode = "",
                            SignPicPath = "",
                            Lang = "TH",
                            AccountId = 1,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = null,
                            ModifiedBy = null,
                            ADTitle = null,
                            DivisionId = (getdiv != null && getdiv.DivisionId != 0) ? (int?)getdiv.DivisionId : null,
                            EmpLevel = getclevel.ToString(),
                            EMPL_RCD = null,
                            EmployeeLevel = null,
                            EffectiveDate = null,
                            Userid_Line = null,

                        };
                        DbWolf.MSTEmployees.InsertOnSubmit(mSTEmployee);
                        DbWolf.SubmitChanges();
                        WriteLogFile("EmpName" + mSTEmployee.NameEn + " Email " + mSTEmployee.Email + " ReportToEmpCode " + mSTEmployee.ReportToEmpCode + " PositionId " + mSTEmployee.PositionId + " DepartmentId " + mSTEmployee.DepartmentId + " DivisionId " + mSTEmployee.DivisionId);
                        WriteLogFile("--------------------------------------------------------------------------------");
                    }



                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLogFile("Error InsertEmpToWolf : " + ex.Message);
                Ext.ErrorLog(ex, "InsertEmpToWolf");

                return false;
            }

        }
        public static bool SyncDepDivPos()
        {
            try
            {
                DataWolfDataContext DbWolf = new DataWolfDataContext(dbWolfConnectionString);

                var data = DbWolf.SyncEmps.ToList();
                var dataDiv = data.GroupBy(x => x.pathName).ToList();
                WriteLogFile("dataDivCount "+ dataDiv.Count);

                var dataDep = data.GroupBy(x => x.orgName).ToList();

                WriteLogFile("dataDepCount " + dataDep.Count);
                var dataPos = data.GroupBy(x => x.jobName).ToList();

                WriteLogFile("dataPosCount " + dataPos.Count);
                var getallDep = DbWolf.MSTDepartments.ToList();
                var getallDiv = DbWolf.MSTDivisions.ToList();
                var getallPos = DbWolf.MSTPositions.ToList();
                WriteLogFile("Start CheckDiv");
                WriteLogFile("---------------------------------Start CheckDiv-----------------------------------------------");
                foreach (var checkdataDiv in dataDiv)
                {
                    var DivData = getallDiv.Where(x => x.NameTh == checkdataDiv.Key).FirstOrDefault();
                    var DivDataInsert = data.Where(x => x.pathName == checkdataDiv.Key).FirstOrDefault();
                    if (DivDataInsert != null)
                    {
                        if (DivData == null)
                        {
                           
                            WriteLogFile("--------------------------------------------------------------------------------");
                            WriteLogFile("Insert Div ");
                            MSTDivision newdiv = new MSTDivision()
                            {
                                NameTh = DivDataInsert.pathName,
                                NameEn = DivDataInsert.pathNameEng,
                                CreatedDate = DateTime.Now,
                                CreatedBy = "System",
                                ModifiedBy = null,
                                ModifiedDate = null,
                                IsActive = true,
                                AccountId = 1,
                                DivisionCode = DivDataInsert.pathShortName
                            };
                            DbWolf.MSTDivisions.InsertOnSubmit(newdiv);
                            DbWolf.SubmitChanges();
                            WriteLogFile("DepName" + newdiv.NameEn + " DepartmentCode " + newdiv.DivisionId);
                            WriteLogFile("--------------------------------------------------------------------------------");
                        }
                        else
                        {
                            WriteLogFile("--------------------------------------------------------------------------------");
                            WriteLogFile("Update Div " + DivData.NameTh +"DivId "+ DivData.DivisionId);

                            DivData.NameTh = DivDataInsert.pathName;
                            DivData.NameEn = DivDataInsert.pathNameEng;
                            DivData.CreatedDate = DivData.CreatedDate;
                            DivData.CreatedBy = "System";
                            DivData.ModifiedBy = "System";
                            DivData.ModifiedDate = DateTime.Now;
                            DivData.IsActive = true;
                            DivData.AccountId = 1;
                            DivData.DivisionCode = DivDataInsert.pathShortName;
                            DbWolf.SubmitChanges();
                            WriteLogFile("DivName" + DivData.NameEn + "DivId " + DivData.DivisionId);
                            WriteLogFile("--------------------------------------------------------------------------------");

                        }
                    }
                    else
                    {
                        WriteLogFile("--------------------------------------------------------------------------------");
                        WriteLogFile("DivDataInsert Null");
                        WriteLogFile("--------------------------------------------------------------------------------");
                    }
                    
                }
                WriteLogFile("---------------------------------End CheckDiv-----------------------------------------------");

                WriteLogFile("---------------------------------Start CheckDep-----------------------------------------------");
                foreach (var checkdataDep in dataDep)
                {
                    var DepData = getallDep.Where(x => x.NameTh == checkdataDep.Key).FirstOrDefault();
                    var DepDataInsert = data.Where(x => x.orgName == checkdataDep.Key).FirstOrDefault();
                    
                    if (DepDataInsert != null)
                    {
                        WriteLogFile("DepDataInsert" + DepDataInsert.orgName);
                        var getDivid = DbWolf.MSTDivisions.Where(x => x.NameTh == DepDataInsert.partName).FirstOrDefault();
                        var getcompany = DbWolf.MSTCompanies.FirstOrDefault();
                        if (DepData == null)
                        {
                            if (getDivid != null)
                            {
                                WriteLogFile("--------------------------------------------------------------------------------");
                                WriteLogFile("Insert Dep "+ DepDataInsert.orgName);
                                MSTDepartment newdep = new MSTDepartment()
                                {
                                    NameTh = DepDataInsert.orgName,
                                    NameEn = DepDataInsert.orgNameEng,
                                    ParentId = null,
                                    DivisionId = getDivid.DivisionId,
                                    DepartmentCode = DepDataInsert.orgShortName,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = "System",
                                    ModifiedBy = "1",
                                    ModifiedDate = null,
                                    IsActive = true,
                                    AccountId = 1,
                                    LeaderId = null,
                                    CompanyCode = getcompany.CompanyCode

                                };
                                DbWolf.MSTDepartments.InsertOnSubmit(newdep);
                                DbWolf.SubmitChanges();
                                WriteLogFile("DepName" + newdep.NameEn + " DepartmentCode " + newdep.DepartmentCode);
                                WriteLogFile("--------------------------------------------------------------------------------");
                            }
                            else
                            {
                                WriteLogFile("--------------------------------------------------------------------------------");
                                WriteLogFile("getDivid null");
                                WriteLogFile("--------------------------------------------------------------------------------");
                            }

                        }
                        else
                        {

                            WriteLogFile("Update Dep " + DepData);
                            WriteLogFile("--------------------------------------------------------------------------------");
                            DepData.NameTh = DepDataInsert.orgName;
                            DepData.NameEn = DepDataInsert.orgNameEng;
                            DepData.ParentId = null;
                            DepData.DivisionId = getDivid.DivisionId;
                            DepData.DepartmentCode = DepDataInsert.orgShortName;
                            DepData.CreatedDate = DateTime.Now;
                            DepData.CreatedBy = "System";
                            DepData.ModifiedBy = "1";
                            DepData.ModifiedDate = null;
                            DepData.IsActive = true;
                            DepData.AccountId = 1;
                            DepData.LeaderId = null;
                            DepData.CompanyCode = getcompany.CompanyCode;
                            WriteLogFile("DepName" + DepData.NameEn + " DepartmentCode " + DepData.DepartmentCode);
                            WriteLogFile("--------------------------------------------------------------------------------");

                        }
                    }else
                    {
                        WriteLogFile("--------------------------------------------------------------------------------");
                        WriteLogFile("DepDataInsert Null");
                        WriteLogFile("--------------------------------------------------------------------------------");
                    }


                }
                WriteLogFile("End CheckDep");

                foreach (var checkdataPos in dataPos)
                {
                    var PosData = getallPos.Where(x => x.NameTh == checkdataPos.Key).FirstOrDefault();
                    var PosDataInsert = data.Where(x => x.jobName == checkdataPos.Key).FirstOrDefault();
                    var getcompany = DbWolf.MSTCompanies.FirstOrDefault();
                    if (PosDataInsert != null)
                    {
                        if (PosData == null)
                        {
                            MSTPosition newdiv = new MSTPosition()
                            {
                                NameTh = PosDataInsert.jobName,
                                NameEn = PosDataInsert.jobNameEng,
                                CreatedDate = DateTime.Now,
                                CreatedBy = "System",
                                ModifiedBy = null,
                                ModifiedDate = null,
                                IsActive = true,
                                AccountId = 1,
                                CompanyCode = getcompany.CompanyCode
                            };
                            DbWolf.MSTPositions.InsertOnSubmit(newdiv);
                            DbWolf.SubmitChanges();
                        }
                        else
                        {
                            WriteLogFile("Update Pos " + PosData);

                            PosData.NameTh = PosDataInsert.jobName;
                            PosData.NameEn = PosDataInsert.jobNameEng;
                            PosData.CreatedDate = PosData.CreatedDate;
                            PosData.CreatedBy = "System";
                            PosData.ModifiedBy = "System";
                            PosData.ModifiedDate = DateTime.Now;
                            PosData.IsActive = true;
                            PosData.AccountId = 1;
                            PosData.CompanyCode = getcompany.CompanyCode;
                            DbWolf.SubmitChanges();

                        }
                    }
                    else
                    {
                        WriteLogFile("PosDataInsert Null");
                    }
                    WriteLogFile("End CheckPos");
                }

                return true;

            }
            catch (Exception ex)
            {
                WriteLogFile("Error SyncDepDivPos : " + ex.Message);
                Ext.ErrorLog(ex, "SyncDepDivPos");
                return false;
            }

        }

        public static bool BackupSyncEmpToBackDataSyncEmp(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. ดึงข้อมูลจาก SyncEmp
                        var dataTable = new DataTable();
                        var command = new SqlCommand("SELECT * FROM SyncEmp", connection, tran);
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        // 2. ใช้ SqlBulkCopy พร้อม Transaction
                        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, tran))
                        {
                            bulkCopy.DestinationTableName = "BackDataSyncEmp";

                            bulkCopy.WriteToServer(dataTable);
                        }

                        tran.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        Console.WriteLine("Rollback due to error: " + ex.Message);
                        WriteLogFile("Error BackupSyncEmpToBackDataSyncEmp : " + ex.Message);
                        return false;
                    }
                }
            }


        }
        public static string _LogFile = ConfigurationManager.AppSettings["LogFile"];
        public static void WriteLogFile(String iText)
        {

            String LogFilePath = String.Format("{0}{1}_Log.txt", _LogFile, DateTime.Now.ToString("yyyyMMdd"));

            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(LogFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    //sbLog.AppendLine(String.Empty);
                    //sbLog.AppendLine(String.Format("--------------- Start Time ({0}) ---------------", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                    String[] ListText = iText.Split('|').ToArray();

                    foreach (String s in ListText)
                    {
                        sbLog.AppendLine(s);
                    }

                    //sbLog.AppendLine(String.Format("--------------- End Time ({0})   ---------------", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
                    //sbLog.AppendLine(String.Empty);

                    outfile.WriteLine(sbLog.ToString());
                }
            }
            catch { }
        }
    }
}
