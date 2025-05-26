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
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        WriteLogFile("4: Preparing request...");

                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var jsonBody = @"{
""outFields"": ""assistId,assistName,assistNameEng,assistShortName,assistShortNameEng,cLevel,costCenter,depId,depName,depNameEng,depShortName,depShortNameEng,divId,divName,divNameEng,divShortName,divShortNameEng,email,empId,empPicture,firstName,firstNameEng,isCommander,isContractEmployee,isNormalPeriod,jobId,jobName,jobNameEng,jobShortName,jobShortNameEng,lastName,lastNameEng,mcJobName,orgDisplayName,orgId,orgLabel,orgLevelId,orgName,orgNameEng,orgShortName,orgShortNameEng,partId,partName,partNameEng,partShortName,partShortNameEng,pathId,pathName,pathNameEng,pathShortName,pathShortNameEng,posId,posName,posNameEng,posShortName,posShortNameEng,prefix,prefixEng,secId,secName,secNameEng,secShortName,secShortNameEng,tel,telExtension,telInternalPrefix,telInternalSuffix,telOfficial,uuid""}";

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
                    var getEmpidReportTo = new MSTEmployee();
                    if (checkEmp != null)
                    {
                        WriteLogFile("Update Emp " + data.empId);
                        SyncEmp getReportTo = new SyncEmp();

                        if (getclevel <= 7)
                        {
                            getReportTo = getdata.Where(x => x.secId == data.secId && x.cLevel == "8").FirstOrDefault();
                        }
                        else if (getclevel == 8)
                        {
                            getReportTo = getdata.Where(x => x.divId == data.divId && x.cLevel == "9").FirstOrDefault();
                        }
                        else if (getclevel == 9)
                        {
                            getReportTo = getdata.Where(x => x.divId == data.divId && x.cLevel == "10").FirstOrDefault();
                        }
                        else if (getclevel == 10)
                        {
                            getReportTo = getdata.Where(x => x.divId == data.divId && x.cLevel == "11").FirstOrDefault();

                        }
                        else if (getclevel == 11)
                        {
                            getReportTo = getdata.Where(x => x.depId == data.depId && x.cLevel == "12").FirstOrDefault();
                        }
                        else if (getclevel == 12)
                        {
                            getReportTo = getdata.Where(x => x.depId == data.depId && x.cLevel == "13").FirstOrDefault();
                        }
                        if (getReportTo != null)
                        {
                            getEmpidReportTo = DbWolf.MSTEmployees.Where(x => x.EmployeeCode == getReportTo.empId).FirstOrDefault();
                        }

                        checkEmp.Username = data.email;
                        checkEmp.NameTh = data.prefix + " " + data.firstName + " " + data.lastName;
                        checkEmp.Email = data.email;
                        checkEmp.NameEn = data.prefixEng + " " + data.firstNameEng + " " + data.lastNameEng;
                        checkEmp.PositionId = data.jobId;
                        checkEmp.DepartmentId = data.depId;
                        checkEmp.ReportToEmpCode = getEmpidReportTo != null ? getEmpidReportTo.EmployeeId.ToString() : null;
                        checkEmp.ModifiedDate = DateTime.Now;
                        checkEmp.ModifiedBy = "System";
                        checkEmp.DivisionId = data.divId;
                        checkEmp.EmpLevel = getclevel.ToString();
                        DbWolf.SubmitChanges();

                    }
                    else
                    {
                        WriteLogFile("Insert Emp " + data.empId);
                        MSTEmployee mSTEmployee = new MSTEmployee()
                        {
                            EmployeeCode = data.empId,
                            Username = data.email,
                            NameTh = data.prefix + " " + data.firstName + " " + data.lastName,
                            NameEn = data.prefixEng + " " + data.firstNameEng + " " + data.lastNameEng,
                            Email = data.email,
                            IsActive = true,
                            PositionId = data.jobId,
                            DepartmentId = data.depId,
                            ReportToEmpCode = "",
                            SignPicPath = "",
                            Lang = "TH",
                            AccountId = 1,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = null,
                            ModifiedBy = null,
                            ADTitle = null,
                            DivisionId = data.divId,
                            EmpLevel = getclevel.ToString(),
                            EMPL_RCD = null,
                            EmployeeLevel = null,
                            EffectiveDate = null,
                            Userid_Line = null,

                        };
                        DbWolf.MSTEmployees.InsertOnSubmit(mSTEmployee);
                        DbWolf.SubmitChanges();
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
                var dataDiv = data.GroupBy(x => x.partName).ToList();
                var dataDep = data.GroupBy(x => x.orgName).ToList();
                var dataPos = data.GroupBy(x => x.jobName).ToList();
                var getallDep = DbWolf.MSTDepartments.ToList();
                var getallDiv = DbWolf.MSTDivisions.ToList();
                var getallPos = DbWolf.MSTPositions.ToList();
                WriteLogFile("Start CheckDiv");
                foreach (var checkdataDiv in dataDiv)
                {
                    var DivData = getallDiv.Where(x => x.NameTh == checkdataDiv.Key).FirstOrDefault();
                    var DivDataInsert = data.Where(x => x.partName == checkdataDiv.Key).FirstOrDefault();
                    if (DivData == null)
                    {
                        MSTDivision newdiv = new MSTDivision()
                        {
                            NameTh = DivDataInsert.partName,
                            NameEn = DivDataInsert.partNameEng,
                            CreatedDate = DateTime.Now,
                            CreatedBy = "System",
                            ModifiedBy = null,
                            ModifiedDate = null,
                            IsActive = true,
                            AccountId = 1,
                            DivisionCode = DivDataInsert.partShortName
                        };
                        DbWolf.MSTDivisions.InsertOnSubmit(newdiv);
                        DbWolf.SubmitChanges();
                    }
                    else
                    {
                        WriteLogFile("Update Div " + DivData);

                        DivData.NameTh = DivDataInsert.partName;
                        DivData.NameEn = DivDataInsert.partNameEng;
                        DivData.CreatedDate = DivData.CreatedDate;
                        DivData.CreatedBy = "System";
                        DivData.ModifiedBy = "System";
                        DivData.ModifiedDate = DateTime.Now;
                        DivData.IsActive = true;
                        DivData.AccountId = 1;
                        DivData.DivisionCode = DivDataInsert.partShortName;
                        DbWolf.SubmitChanges();

                    }

                    WriteLogFile("End CheckDiv");


                }
                WriteLogFile("Start CheckDep");
                foreach (var checkdataDep in dataDep)
                {
                    var DepData = getallDep.Where(x => x.NameTh == checkdataDep.Key).FirstOrDefault();
                    var DepDataInsert = data.Where(x => x.partName == checkdataDep.Key).FirstOrDefault();
                    var getDivid = DbWolf.MSTDepartments.Where(x => x.NameTh == DepDataInsert.orgName).FirstOrDefault();
                    var getcompany = DbWolf.MSTCompanies.FirstOrDefault();
                    if (DepData == null)
                    {
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
                    }
                    else
                    {
                        WriteLogFile("Update Dep " + DepData);

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

                    }

                }
                WriteLogFile("End CheckDep");


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
