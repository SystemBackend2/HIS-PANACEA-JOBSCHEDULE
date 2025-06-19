using Dapper;
using System.Data.SqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Text;
using System.Threading;
using System.Transactions;
using Panaceachs_api;
using Jobschedule;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;


int count = 0;
while (true)
{

    // ทำสิ่งที่ต้องการทำทุก 5 วินาที
    DoSomethingAsync();
    InsPicAsync();
    UpdRejectAsync();
    UpdAcceptAsync();
    Console.WriteLine("อ่านผลเสร็จสิ้น");
    // หน่วงเวลา 5 วินาที
   
    Thread.Sleep(60000);

    count++;
    if (count % 5 == 0) // ทุก 5 นาที (5 วินาที x 12 = 60 วินาที)
    {
        await Sendversion();
    }
}



static async Task Sendversion()
{
    string username = "10829-gw2";
    string password = "LTsIb53Wv00f1oLe056J";

    // Step 1: Login and retrieve token
    string token = await Login(username, password);
    Console.WriteLine("Token: " + token);

    // Step 2: Send heartbeat with token
    await SendHeartbeat(token);
}

 static async Task<string> Login(string username, string password)
{
    var loginUrl = "https://sync-api.hie-rayong.everapp.io/v2/user/login";
    var loginData = new
    {
        username,
        password
    };

    var json = JsonConvert.SerializeObject(loginData);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    HttpClient client = new HttpClient();
    var response = await client.PostAsync(loginUrl, content);
    response.EnsureSuccessStatusCode();

    var responseString = await response.Content.ReadAsStringAsync();
    dynamic responseObject = JsonConvert.DeserializeObject(responseString);

    SendHeartbeat(responseObject.token);

    return responseObject.token;

    
}

 static async Task SendHeartbeat(string token)
{

    try
    {
        var heartbeatUrl = "https://sync-api.hie-rayong.everapp.io/v2/user/heartbeat";
        var heartbeatData = new
        {
            GatewayVersion = "1.0.0"
        };

        var json = JsonConvert.SerializeObject(heartbeatData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync(heartbeatUrl, content);
        response.EnsureSuccessStatusCode();

        Console.WriteLine("Heartbeat sent successfully!");
    }
    catch { 
    
    }
    
}

static async Task DoSomethingAsync()
{


// ตัวอย่าง: พิมพ์ข้อความทุก 5 วินาที
string directoryPath = "\\\\10.99.0.21\\TopProvider HIS\\Result";


    NetworkCredential credentials = new NetworkCredential("tophis", "top#1234");
    string networkPath = directoryPath;

    // Connect to the network path using the provided credentials
    using (new Panaceachs_api.NetworkConnection(networkPath, credentials))
    {

        if (!Directory.Exists(networkPath))
        {
            throw new DirectoryNotFoundException($"The directory path {networkPath} is not accessible.");
        }


        string connectionString = "Data Source=10.4.0.91;Initial Catalog=panacea_prd;User ID=devicon;Password=ic0nte@m2;";


        SqlConnection connection = null;
        if (!string.IsNullOrEmpty(connectionString))
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }


        // เปิดการเชื่อมต่อกับฐานข้อมูล


        // หาทุกไฟล์ในไดเรกทอรี
        string[] files = Directory.GetFiles(directoryPath);

        for (int i = 0; i < files.Length; i++)
        {

            SqlTransaction transaction = null;
            // SqlTransaction transaction = connection.BeginTransaction();
            string file = files[i];
            try
            {

                int success = 0;



                // Console.WriteLine(Path.GetFileName(file));

                var TMP = File.ReadAllText(file);
                var OBR = TMP.Split("OBR");

                // Check if OBR length is less than or equal to 1
                if (OBR.Length <= 1)
                {
                    throw new Exception($"Invalid file format in {file}: OBR segment is missing or malformed.");
                }

                for (int j = 1; j < OBR.Length; j++)
                {
                    if (transaction == null)
                    {
                        transaction = connection.BeginTransaction();
                    }
                    var item = OBR[j];
                    // แยกสตริง OBR ด้วยเครื่องหมาย |
                    var parts = item.Split('|');
                    if (parts.Length > 4)
                    {
                        var orderid = parts[2];
                        var testcode = parts[4].Split("^")[0];

                        // สร้างคำสั่ง SQL โดยใช้ข้อมูลจาก OBR
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine($@"SELECT * FROM TB_FINANCES AS F WITH (NOLOCK) 
                                WHERE F.orderid = @orderid  AND F.financetype = 'L' and code = @testcode  ");
                        var finances = await connection.QueryAsync<TBFinances>(sql.ToString(), new { orderid = orderid, testcode = testcode }, transaction);

                        // Check if no records were found
                        if (!finances.Any())
                        {
                            throw new Exception($"No finance records found for Order ID: {orderid} and Test Code: {testcode}.");
                        }

                        //    sql.Clear();
                        //    sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS AS L WITH (NOLOCK) 
                        //WHERE  L.code = @testcode and CancelFlag is  null ");
                        //    var Getlab = await dbConnection.QueryAsync<TBMLabTests>(sql.ToString(), new { orderid = orderid, testcode = testcode }, transaction);


                        var FinancesId = finances.FirstOrDefault().FinanceId;
                        var ExpenseId = finances.FirstOrDefault().ExpenseId;
                        var OrderId = finances.FirstOrDefault().OrderId;
                        var OrderDate = finances.FirstOrDefault().OrderDate;
                        var OrderTime = finances.FirstOrDefault().OrderTime;

                        sql.Clear();
                        sql.AppendLine($@"SELECT * FROM TBM_EXPENSES AS EP WITH (NOLOCK) 
                                WHERE ExpenseId = @ExpenseId and CancelFlag is  null 
                        ");
                        var Expenseid = await connection.QueryAsync<TBMExpenses>(sql.ToString(), new { ExpenseId = finances.FirstOrDefault().ExpenseId }, transaction);


                        var LabOrderCode = Expenseid.FirstOrDefault().Code;
                        var LabResultCode = testcode; // Code ของ Labtest
                        var RunHn = finances.FirstOrDefault().RunHn;
                        var YearHn = finances.FirstOrDefault().YearHn;
                        var Hn = finances.FirstOrDefault().Hn;
                        var ServiceId = finances.FirstOrDefault().ServiceId;
                        var ClinicId = finances.FirstOrDefault().ClinicId;
                        var AdmitId = finances.FirstOrDefault().AdmitId;
                        var RunAn = finances.FirstOrDefault().RunAn;
                        var YearAn = finances.FirstOrDefault().YearAn;
                        var An = finances.FirstOrDefault().An;
                        var PatientId = finances.FirstOrDefault().PatientId;
                        var Gender = finances.FirstOrDefault().Gender;
                        var OBX = item.Split("OBX");

                        sql.Clear();
                        sql.AppendLine($@"SELECT DATEDIFF(YEAR, Birthdate, GETDATE()) AS Age FROM TB_Patients  WITH (NOLOCK) WHERE patientid = @patientid  ");
                        var Age = await connection.QueryFirstAsync<string>(sql.ToString(), new { patientid = finances.FirstOrDefault().PatientId }, transaction);

                        for (int x = 1; x < OBX.Length; x++)
                        {
                            var item2 = OBX[x];
                            var parts2 = item2.Split('|');
                            if (parts2.Length > 4)
                            {
                                var LabtestCode = parts2[3].Split("^")[0];
                                //010079

                                sql.Clear();
                                sql.AppendLine($@"SELECT count(*) FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE  expenseid = @expenseid and LabcareOutlab = 'Y' ");
                                var checkcountLabcareOutlab = await connection.QuerySingleAsync<int>(sql.ToString(), new { expenseid = ExpenseId }, transaction);

                                if (checkcountLabcareOutlab > 0)
                                {
                                    sql.Clear();
                                    sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE expenseid = @expenseid ");
                                }
                                else
                                {
                                    sql.Clear();
                                    sql.AppendLine($@"SELECT count(*) FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE  expenseid = @expenseid ");
                                    var checkcount = await connection.QuerySingleAsync<int>(sql.ToString(), new { expenseid = ExpenseId }, transaction);

                                    if (checkcount > 1)
                                    {
                                        sql.Clear();
                                        sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE seq = @seq and expenseid = @expenseid ");
                                    }
                                    if (checkcount == 1)
                                    {
                                        sql.Clear();
                                        sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE expenseid = @expenseid ");
                                    }
                                }


                                //sql.Clear();
                                //sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE seq = @seq and expenseid = @expenseid ");
                                var Labtest = await connection.QueryAsync<TbmLabTest>(sql.ToString(), new { seq = LabtestCode, expenseid = ExpenseId }, transaction);
                                var testid = Labtest.FirstOrDefault().TestId;
                                var Domain = Labtest.FirstOrDefault().Domain;
                                var ResultValue = parts2[5];
                                var DataType = "";
                                int NumberValue = 0;
                                var TextValue = "";
                                var INumberValue = "";
                                var VNumberValue = "";

                                if (int.TryParse(ResultValue, out _))
                                {
                                    DataType = "N";
                                    NumberValue = System.Convert.ToInt32(ResultValue);
                                    INumberValue = ",NumberValue";
                                    VNumberValue = ",@NumberValue";
                                }
                                else
                                {
                                    DataType = "T";
                                    TextValue = ResultValue;

                                }

                                var Unit = parts2[6];
                                var ReferencesRange = parts2[7];
                                ReferencesRange = ReferencesRange.Replace(",", "");
                                var AbnormalFlags = parts2[8];

                                //if (AbnormalFlags != "L" || AbnormalFlags != "H" || AbnormalFlags != "LL" || AbnormalFlags != "HH")
                                //{
                                //    AbnormalFlags = "N";
                                //}

                                var ObservResultStatus = parts2[11];
                                var DatetimeOfTheObservation = parts2[14];
                                var ResponsibleObserver = parts2[16];


                                var MinNumberRef = "";
                                var MaxNumberRef = "";

                                if (ReferencesRange.Contains("-"))
                                {
                                    MinNumberRef = ReferencesRange.Split("-")[0];
                                    MaxNumberRef = ReferencesRange.Split("-")[1];
                                }



                                var Iminmaxref = "";
                                var Vminmaxref = "";
                                decimal decimalValue;
                                bool isDecimal = decimal.TryParse(MinNumberRef, out decimalValue);
                                bool isDecimal2 = decimal.TryParse(MaxNumberRef, out decimalValue);
                                if (isDecimal && isDecimal2)
                                {
                                    Iminmaxref = ",MinNumberRef,MaxNumberRef";
                                    Vminmaxref = ",@MinNumberRef,@MaxNumberRef";
                                }

                                var ISeverity = "";
                                var VSeverity = "";
                                if (AbnormalFlags != "")
                                {
                                    ISeverity = ",Severity";
                                    VSeverity = ",@Severity";
                                }




                                sql.Clear();
                                sql.AppendLine($@" 
                                        INSERT INTO TB_LAB_RESULTS (
                                        FinanceId,ExpenseId,OrderId,OrderDate,OrderTime,LabOrderCode,
                                        TestId,LabResultCode,PatientId,RunHn,YearHn,Hn,ServiceId,ClinicId,
                                        AdmitId,RunAn,YearAn,An,Age,Gender,DataType,ResultValue
                                        {INumberValue},TextValue{Iminmaxref}{ISeverity},UserCreated,DateCreated,Domain
                                        ) 
                                        VALUES (
                                        @FinanceId,@ExpenseId,@OrderId,@OrderDate,@OrderTime,@LabOrderCode,
                                        @TestId,@LabResultCode,@PatientId,@RunHn,@YearHn,@Hn,@ServiceId,@ClinicId,
                                        @AdmitId,@RunAn,@YearAn,@An,@Age,@Gender,@DataType,@ResultValue
                                        {VNumberValue},@TextValue{Vminmaxref}{VSeverity},@UserCreated,GETDATE(),@Domain
                                        ) 
                                        
                                ");

                                try
                                {
                                    success = connection.Execute(sql.ToString(), new
                                    {

                                        FinanceId = FinancesId,
                                        ExpenseId = ExpenseId,
                                        OrderId = OrderId,
                                        OrderDate = OrderDate,
                                        OrderTime = OrderTime,
                                        LabOrderCode = LabOrderCode,
                                        TestId = testid,
                                        LabResultCode = LabResultCode,
                                        PatientId = PatientId,
                                        RunHn = RunHn,
                                        YearHn = YearHn,
                                        Hn = Hn,
                                        ServiceId = ServiceId,
                                        ClinicId = ClinicId,
                                        AdmitId = AdmitId,
                                        RunAn = RunAn,
                                        YearAn = YearAn,
                                        An = An,
                                        Age = Age,
                                        Gender = Gender,
                                        DataType = DataType,
                                        ResultValue = ResultValue,
                                        NumberValue = NumberValue,
                                        TextValue = TextValue,
                                        MinNumberRef = MinNumberRef,
                                        MaxNumberRef = MaxNumberRef,
                                        Domain = Domain,
                                        Severity = AbnormalFlags,
                                        Usercreated = "LIS"
                                    }, transaction);
                                }

                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }


                            }
                        }

                    }
                }


                if (success > 0)
                {
                    transaction.Commit();
                    File.Delete(file);
                }



            }
            catch

            {

                Console.WriteLine(Path.GetFileName(file) + " ไฟล์นี้ข้อมูลไม่ถูกต้อง");

                // ย้ายไฟล์ไปยังโฟลเดอร์ WaitingRecheck
                string destinationPath = Path.Combine(directoryPath + "\\WaitingRecheck", Path.GetFileName(file));

                Console.WriteLine(destinationPath);

                if (File.Exists(destinationPath))
                {
                    // If it exists, delete it before moving the new file
                    File.Delete(destinationPath);
                }

                // Move the file to the WaitingRecheck folder
                File.Move(file, destinationPath);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                continue;

            }


        }


        connection.Dispose();
    }

}

static async Task InsPicAsync()
{

    string directoryPath = "\\\\10.99.0.21\\TopProvider HIS\\Result";


    NetworkCredential credentials = new NetworkCredential("tophis", "top#1234");
    string networkPath = directoryPath;

    // Connect to the network path using the provided credentials
    using (new NetworkConnection(networkPath, credentials))
    {

        if (!Directory.Exists(networkPath))
        {
            throw new DirectoryNotFoundException($"The directory path {networkPath} is not accessible.");
        }


        string connectionString = "Data Source=10.4.0.91;Initial Catalog=panacea_prd;User ID=devicon;Password=ic0nte@m2;";


        SqlConnection connection = null;
        if (!string.IsNullOrEmpty(connectionString))
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }




        // หาทุกไฟล์ในไดเรกทอรี
        string[] files = Directory.GetFiles(directoryPath);

        for (int i = 0; i < files.Length; i++)
        {
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {

                int success = 0;


                string file = files[i];
                // Console.WriteLine(Path.GetFileName(file));
                string fileName = Path.GetFileName(file);
                //   string fileName = Path.GetFileNameWithoutExtension(file);
                string fileExtension = Path.GetExtension(file).ToLower(); // ดึงสกุลไฟล์และแปลงเป็นตัวพิมพ์เล็ก

                // ตรวจสอบสกุลไฟล์
                var supportedExtensions = new HashSet<string> { ".jpg", ".png", ".gif", ".pdf", ".xlsx", ".csv" };
                if (!supportedExtensions.Contains(fileExtension))
                {
                    Console.WriteLine($"Skipping unsupported file type: {fileExtension}");
                    continue;
                }


                byte[] fileBytes = File.ReadAllBytes(files[i]);
                string base64String = System.Convert.ToBase64String(fileBytes);

                fileName = fileName.Split(".")[0];
                fileName = fileName.Split("_")[0];

                StringBuilder sql = new StringBuilder();
                sql.AppendLine($@"SELECT * FROM TB_LAB_RESULTS AS F WITH (NOLOCK) 
                                WHERE F.orderid = @orderid  ");
                var TBLabResults = await connection.QueryFirstOrDefaultAsync<TBLabResults>(sql.ToString(), new { orderid = fileName }, transaction);

                if (TBLabResults != null)
                {
                    sql.Clear();
                    sql.AppendLine($@" Insert into TB_LAB_RESULT_PICTURES (
      [LabResultId]
      ,[Picture]
      ,[Seq]
      ,[UserCreated]
      ,[DateCreated],[FileType],[FileName]) values (
      @LabResultId
      ,@Picture
      ,@Seq
      ,@UserCreated
      ,Getdate(),@FileType,@FileName)    ");
                    success = await connection.ExecuteAsync(sql.ToString(), new
                    {
                        LabResultId = TBLabResults.LabResultId,
                        Picture = fileBytes,
                        Seq = 1,
                        UserCreated = "LIS",
                        FileType = fileExtension,
                        FileName = fileName
                    }, transaction);
                }


                if (success > 0)
                {
                    transaction.Commit();
                    File.Delete(file);
                }


            }
            catch
            {
                continue;
            }


        }

        connection.Dispose();

    }

}

static async Task UpdRejectAsync()
{
    // ตัวอย่าง: พิมพ์ข้อความทุก 5 วินาที
    string directoryPath = "\\\\10.99.0.21\\TopProvider HIS\\Reject";


    NetworkCredential credentials = new NetworkCredential("tophis", "top#1234");
    string networkPath = directoryPath;

    // Connect to the network path using the provided credentials
    using (new Panaceachs_api.NetworkConnection(networkPath, credentials))
    {

        if (!Directory.Exists(networkPath))
        {
            throw new DirectoryNotFoundException($"The directory path {networkPath} is not accessible.");
        }


        string connectionString = "Data Source=10.4.0.91;Initial Catalog=panacea_prd;User ID=devicon;Password=ic0nte@m2;";


        SqlConnection connection = null;
        if (!string.IsNullOrEmpty(connectionString))
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }



        // เปิดการเชื่อมต่อกับฐานข้อมูล


        // หาทุกไฟล์ในไดเรกทอรี
        string[] files = Directory.GetFiles(directoryPath);

        for (int i = 0; i < files.Length; i++)
        {
            SqlTransaction transaction = connection.BeginTransaction();

            string file = files[i];
            try
            {

                int success = 0;


                var TMP = File.ReadAllText(file);
                var CA = TMP.Split("CA");

                for (int j = 1; j < CA.Length; j++)
                {
                    var item = CA[j];
                    // แยกสตริง OBR ด้วยเครื่องหมาย |
                    var parts = item.Split('|');
                    if (parts.Length > 0)
                    {
                        var orderid = parts[1];

                        // สร้างคำสั่ง SQL โดยใช้ข้อมูลจาก OBR
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine($@"UPDATE TB_FINANCES SET [STATUS] = 'X'
                                WHERE orderid = @orderid  AND  financetype = 'L'  ");
                        success = await connection.ExecuteAsync(sql.ToString(), new { orderid = orderid }, transaction);

                    }
                }


                if (success > 0)
                {
                    transaction.Commit();
                    File.Delete(file);
                }
                else
                {
                    transaction.Rollback();
                }

            }
            catch

            {
                Console.WriteLine(Path.GetFileName(file) + " ไฟล์นี้ข้อมูลไม่ถูกต้อง");

                // ย้ายไฟล์ไปยังโฟลเดอร์ WaitingRecheck
                string destinationPath = Path.Combine(directoryPath + "\\WaitingRecheck", Path.GetFileName(file));

                Console.WriteLine(destinationPath);

                if (File.Exists(destinationPath))
                {
                    // If it exists, delete it before moving the new file
                    File.Delete(destinationPath);
                }

                // Move the file to the WaitingRecheck folder
                File.Move(file, destinationPath);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                continue;


            }


        }
        connection.Dispose();
    }
}

static async Task UpdAcceptAsync()
{
    // ตัวอย่าง: พิมพ์ข้อความทุก 5 วินาที
    string directoryPath = "\\\\10.99.0.21\\TopProvider HIS\\Accept";


    NetworkCredential credentials = new NetworkCredential("tophis", "top#1234");
    string networkPath = directoryPath;

    // Connect to the network path using the provided credentials
    using (new Panaceachs_api.NetworkConnection(networkPath, credentials))
    {

        if (!Directory.Exists(networkPath))
        {
            throw new DirectoryNotFoundException($"The directory path {networkPath} is not accessible.");
        }


        string connectionString = "Data Source=10.4.0.91;Initial Catalog=panacea_prd;User ID=devicon;Password=ic0nte@m2;";


        SqlConnection connection = null;
        if (!string.IsNullOrEmpty(connectionString))
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }



        // เปิดการเชื่อมต่อกับฐานข้อมูล


        // หาทุกไฟล์ในไดเรกทอรี
        string[] files = Directory.GetFiles(directoryPath);

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            SqlTransaction transaction = connection.BeginTransaction();
            try
            {

                int success = 0;


                var TMP = File.ReadAllText(file);
                var CA = TMP.Split("AA");

                for (int j = 1; j < CA.Length; j++)
                {
                    var item = CA[j];
                    // แยกสตริง OBR ด้วยเครื่องหมาย |
                    var parts = item.Split('|');
                    if (parts.Length > 0)
                    {
                        var orderid = parts[1];

                        // สร้างคำสั่ง SQL โดยใช้ข้อมูลจาก OBR
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine($@"UPDATE TB_FINANCES SET [STATUS] = 'A' , DateAccepted = getdate()
                                WHERE orderid = @orderid  AND  financetype = 'L'  ");
                        success = await connection.ExecuteAsync(sql.ToString(), new { orderid = orderid }, transaction);

                    }
                }


                if (success > 0)
                {
                    transaction.Commit();
                    File.Delete(file);
                    // connection.Dispose();
                }
                else
                {
                    transaction.Rollback();
                }

            }
            catch

            {
                Console.WriteLine(Path.GetFileName(file) + " ไฟล์นี้ข้อมูลไม่ถูกต้อง");

                // ย้ายไฟล์ไปยังโฟลเดอร์ WaitingRecheck
                string destinationPath = Path.Combine(directoryPath + "\\WaitingRecheck", Path.GetFileName(file));

                Console.WriteLine(destinationPath);

                if (File.Exists(destinationPath))
                {
                    // If it exists, delete it before moving the new file
                    File.Delete(destinationPath);
                }

                // Move the file to the WaitingRecheck folder
                File.Move(file, destinationPath);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                continue;


            }


        }

        connection.Dispose();
    }
}

static async Task LoginLabDoSomethingAsync()
{

    try
    {
        // ตัวอย่าง: พิมพ์ข้อความทุก 5 วินาที
        string directoryPath = "\\\\10.99.0.20\\LogInS_Paracea\\RES";


        //NetworkCredential credentials = new NetworkCredential("10.99.0.20\\loginsparacea", "loginsparacea");
        string networkPath = directoryPath;

        var credentials = new NetworkCredential("10.99.0.20\\loginsparacea", "loginsparacea", "10.99.0.20");


        // Connect to the network path using the provided credentials
        //   using (new Panaceachs_api.NetworkConnection(networkPath, credentials))
        //  {

        if (!Directory.Exists(networkPath))
        {
            //throw new DirectoryNotFoundException($"The directory path {networkPath} is not accessible.");
            var connect = new Panaceachs_api.NetworkConnection(networkPath, credentials);

        }


        string connectionString = "Data Source=10.4.0.91;Initial Catalog=panacea_prd;User ID=devicon;Password=ic0nte@m2;";


        SqlConnection connection = null;
        if (!string.IsNullOrEmpty(connectionString))
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }


        // เปิดการเชื่อมต่อกับฐานข้อมูล


        // หาทุกไฟล์ในไดเรกทอรี
        string[] files = Directory.GetFiles(directoryPath);

        for (int i = 0; i < files.Length; i++)
        {
            SqlTransaction transaction = connection.BeginTransaction();
            string file = files[i];
            try
            {

                int success = 0;
                // Console.WriteLine(Path.GetFileName(file));

                var TMP = File.ReadAllText(file);
                var OBR = TMP.Split("OBR");

                for (int j = 1; j < OBR.Length; j++)
                {
                    var item = OBR[j];
                    // แยกสตริง OBR ด้วยเครื่องหมาย |
                    var parts = item.Split('|');
                    if (parts.Length > 4)
                    {
                        var orderid = parts[2];
                        var testcode = parts[4].Split("^")[0];

                        // สร้างคำสั่ง SQL โดยใช้ข้อมูลจาก OBR
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine($@"SELECT * FROM TB_FINANCES AS F WITH (NOLOCK) 
                                WHERE F.orderid = @orderid  AND F.financetype = 'L' and code = @testcode  ");
                        var finances = await connection.QueryAsync<TBFinances>(sql.ToString(), new { orderid = orderid, testcode = testcode }, transaction);


                        //    sql.Clear();
                        //    sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS AS L WITH (NOLOCK) 
                        //WHERE  L.code = @testcode and CancelFlag is  null ");
                        //    var Getlab = await dbConnection.QueryAsync<TBMLabTests>(sql.ToString(), new { orderid = orderid, testcode = testcode }, transaction);


                        var FinancesId = finances.FirstOrDefault().FinanceId;
                        var ExpenseId = finances.FirstOrDefault().ExpenseId;
                        var OrderId = finances.FirstOrDefault().OrderId;
                        var OrderDate = finances.FirstOrDefault().OrderDate;
                        var OrderTime = finances.FirstOrDefault().OrderTime;

                        sql.Clear();
                        sql.AppendLine($@"SELECT * FROM TBM_EXPENSES AS EP WITH (NOLOCK) 
                                WHERE ExpenseId = @ExpenseId and CancelFlag is  null 
                        ");
                        var Expenseid = await connection.QueryAsync<TBMExpenses>(sql.ToString(), new { ExpenseId = finances.FirstOrDefault().ExpenseId }, transaction);


                        var LabOrderCode = Expenseid.FirstOrDefault().Code;
                        var LabResultCode = testcode; // Code ของ Labtest
                        var RunHn = finances.FirstOrDefault().RunHn;
                        var YearHn = finances.FirstOrDefault().YearHn;
                        var Hn = finances.FirstOrDefault().Hn;
                        var ServiceId = finances.FirstOrDefault().ServiceId;
                        var ClinicId = finances.FirstOrDefault().ClinicId;
                        var AdmitId = finances.FirstOrDefault().AdmitId;
                        var RunAn = finances.FirstOrDefault().RunAn;
                        var YearAn = finances.FirstOrDefault().YearAn;
                        var An = finances.FirstOrDefault().An;
                        var PatientId = finances.FirstOrDefault().PatientId;
                        var Gender = finances.FirstOrDefault().Gender;
                        var OBX = item.Split("OBX");

                        sql.Clear();
                        sql.AppendLine($@"SELECT DATEDIFF(YEAR, Birthdate, GETDATE()) AS Age FROM TB_Patients  WITH (NOLOCK) WHERE patientid = @patientid  ");
                        var Age = await connection.QueryFirstAsync<string>(sql.ToString(), new { patientid = finances.FirstOrDefault().PatientId }, transaction);

                        for (int x = 1; x < OBX.Length; x++)
                        {
                            var item2 = OBX[x];
                            var parts2 = item2.Split('|');
                            if (parts2.Length > 4)
                            {
                                var LabtestCode = parts2[3].Split("^")[0];
                                //010079

                                sql.Clear();
                                sql.AppendLine($@"SELECT count(*) FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE  expenseid = @expenseid ");
                                var checkcount = await connection.QuerySingleAsync<int>(sql.ToString(), new { expenseid = ExpenseId }, transaction);

                                if (checkcount > 1)
                                {
                                    sql.Clear();
                                    sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE seq = @seq and expenseid = @expenseid ");
                                }
                                if (checkcount == 1)
                                {
                                    sql.Clear();
                                    sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE expenseid = @expenseid ");
                                }

                                //sql.Clear();
                                //sql.AppendLine($@"SELECT * FROM TBM_LAB_TESTS  WITH (NOLOCK) WHERE seq = @seq and expenseid = @expenseid ");
                                var Labtest = await connection.QueryAsync<TbmLabTest>(sql.ToString(), new { seq = LabtestCode, expenseid = ExpenseId }, transaction);
                                var testid = Labtest.FirstOrDefault().TestId;
                                var Domain = Labtest.FirstOrDefault().Domain;
                                var ResultValue = parts2[5];
                                var DataType = "";
                                int NumberValue = 0;
                                var TextValue = "";
                                var INumberValue = "";
                                var VNumberValue = "";

                                if (int.TryParse(ResultValue, out _))
                                {
                                    DataType = "N";
                                    NumberValue = System.Convert.ToInt32(ResultValue);
                                    INumberValue = ",NumberValue";
                                    VNumberValue = ",@NumberValue";
                                }
                                else
                                {
                                    DataType = "T";
                                    TextValue = ResultValue;

                                }

                                var Unit = parts2[6];
                                var ReferencesRange = parts2[7];
                                var AbnormalFlags = parts2[8];

                                //if (AbnormalFlags != "L" || AbnormalFlags != "H" || AbnormalFlags != "LL" || AbnormalFlags != "HH")
                                //{
                                //    AbnormalFlags = "N";
                                //}

                                var ObservResultStatus = parts2[11];
                                var DatetimeOfTheObservation = parts2[14];
                                var ResponsibleObserver = parts2[16];


                                var MinNumberRef = "";
                                var MaxNumberRef = "";

                                if (ReferencesRange.Contains("-"))
                                {
                                    MinNumberRef = ReferencesRange.Split("-")[0];
                                    MaxNumberRef = ReferencesRange.Split("-")[1];
                                }


                                var Iminmaxref = "";
                                var Vminmaxref = "";
                                decimal decimalValue;
                                bool isDecimal = decimal.TryParse(MinNumberRef, out decimalValue);
                                bool isDecimal2 = decimal.TryParse(MaxNumberRef, out decimalValue);
                                if (isDecimal && isDecimal2)
                                {
                                    Iminmaxref = ",MinNumberRef,MaxNumberRef";
                                    Vminmaxref = ",@MinNumberRef,@MaxNumberRef";
                                }

                                var ISeverity = "";
                                var VSeverity = "";
                                if (AbnormalFlags != "")
                                {
                                    ISeverity = ",Severity";
                                    VSeverity = ",@Severity";
                                }


                                sql.Clear();
                                sql.AppendLine($@" 
                                        INSERT INTO TB_LAB_RESULTS (
                                        FinanceId,ExpenseId,OrderId,OrderDate,OrderTime,LabOrderCode,
                                        TestId,LabResultCode,PatientId,RunHn,YearHn,Hn,ServiceId,ClinicId,
                                        AdmitId,RunAn,YearAn,An,Age,Gender,DataType,ResultValue
                                        {INumberValue},TextValue{Iminmaxref}{ISeverity},UserCreated,DateCreated,Domain
                                        ) 
                                        VALUES (
                                        @FinanceId,@ExpenseId,@OrderId,@OrderDate,@OrderTime,@LabOrderCode,
                                        @TestId,@LabResultCode,@PatientId,@RunHn,@YearHn,@Hn,@ServiceId,@ClinicId,
                                        @AdmitId,@RunAn,@YearAn,@An,@Age,@Gender,@DataType,@ResultValue
                                        {VNumberValue},@TextValue{Vminmaxref}{VSeverity},@UserCreated,GETDATE(),@Domain
                                        ) 
                                        
                                ");

                                success = connection.Execute(sql.ToString(), new
                                {
                                    FinanceId = FinancesId,
                                    ExpenseId = ExpenseId,
                                    OrderId = OrderId,
                                    OrderDate = OrderDate,
                                    OrderTime = OrderTime,
                                    LabOrderCode = LabOrderCode,
                                    TestId = testid,
                                    LabResultCode = LabResultCode,
                                    PatientId = PatientId,
                                    RunHn = RunHn,
                                    YearHn = YearHn,
                                    Hn = Hn,
                                    ServiceId = ServiceId,
                                    ClinicId = ClinicId,
                                    AdmitId = AdmitId,
                                    RunAn = RunAn,
                                    YearAn = YearAn,
                                    An = An,
                                    Age = Age,
                                    Gender = Gender,
                                    DataType = DataType,
                                    ResultValue = ResultValue,
                                    NumberValue = NumberValue,
                                    TextValue = TextValue,
                                    MinNumberRef = MinNumberRef,
                                    MaxNumberRef = MaxNumberRef,
                                    Domain = Domain,
                                    Severity = AbnormalFlags,
                                    Usercreated = "LIS"
                                }, transaction);
                            }
                        }
                    }
                }


                if (success > 0)
                {
                    transaction.Commit();
                    File.Delete(file);
                }
            }
            catch

            {

                Console.WriteLine(Path.GetFileName(file) + " ไฟล์นี้ข้อมูลไม่ถูกต้อง");
                continue;

            }
        }

        connection.Dispose();
        // }

        /// usingnetwork

    }

    catch (Exception ex)
    {
        var err = ex;
    }


}


