using EmailResponseService.Model;
using EmailThreadDemo;
using EmailThreadDemo.Model;

namespace EmailThreadTest;

public class Tests
{
    private AccountInfo _accountInfo;
    private MailSettings _mailSettings;
    
    [SetUp]
    public void Setup()
    {
        #region Google 帳戶
        var accountInfo = new AccountInfo
        {
            Username = "louisding70121@gmail.com",
            Password = "dznvgtiefzqhglev"
        };
        
        var mailSettings = new MailSettings
        {
            // IMAP (收信)
            Host = "imap.gmail.com",
            Port = 993,
            UseSsl = true,
            // SMTP (發信)
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 465,
            SmtpUseSsl = true
        };
        
        #endregion

        #region Outlook 帳戶(驗證未過)

        // var accountInfo = new AccountInfo
        // {
        //     Username = "louisding@tpdrg.com",
        //     Password = "dznvgtiefzqhglev"
        // };
        //
        // var mailSettings = new MailSettings
        // {
        //     // IMAP (收信)
        //     Host = "outlook.office365.com",
        //     Port = 993,
        //     UseSsl = true,
        //
        //     // SMTP (發信)
        //     SmtpHost = "smtp.office365.com",
        //     SmtpPort = 587,
        //     SmtpUseSsl = true
        // };

        #endregion
        
        _accountInfo = accountInfo;
        _mailSettings = mailSettings;
    }

    //測試發送功能
    [Test]
    public void SendMailTest()
    {
        #region 寄送

        var mail = new EmailMessageModel
        {
            Subject = "測試信件",
            TextBody = "這是一封測試信",
            HtmlBody = "<h1>這是一封測試信</h1><p>Hello MailKit</p>",
            Priority = "High",
            RequestReadReceipt = false
        };

        mail.From.Add(new EmailAddressModel
        {
            Name = "Louis",
            Address = "louisding70121@gmail.com"
        });

        mail.To.Add(new EmailAddressModel
        {
            Name = "LouisA",
            Address = "louistpdrgtesta@gmail.com"
        });
        
        ////附加檔案(暫時用不到，先註解掉)
        // mail.Attachments.Add(new EmailAttachmentModel
        // {
        //     FileName = "test.pdf",
        //     FilePath = @"D:\Temp\test.pdf",
        //     ContentType = "application/pdf",
        //     IsInline = false
        // });
        
        var mailFun = new MailFun(_accountInfo, _mailSettings);
        mailFun.SendMail(mail);

        #endregion

        #region 驗證

        //待做

        #endregion
        
    }
    
    //測試發送功能(表格)
    [Test]
    public void SendTableMailTest()
    {
        #region 寄送

        var mail = new EmailMessageModel
        {
            Subject = "測試信件2(Table)",
            TextBody = "這是一封測試信",
            HtmlBody = """
                       <table border="1">
                         <thead>
                           <tr>
                             <th>Product</th>
                             <th>Currency</th>
                             <th>BBG Code 1</th>
                             <th>BBG Code 2</th>
                             <th>BBG Code 3</th>
                             <th>BBG Code 4</th>
                             <th>BBG Code 5</th>
                             <th>GP</th>
                             <th>Strike (%)</th>
                             <th>KO Type</th>
                             <th>KO Barrier (%)</th>
                             <th>Coupon p.a. (%)</th>
                             <th>Upfront / NotePrice (%)</th>
                             <th>Tenor (m)</th>
                             <th>Barrier Type</th>
                             <th>KI Barrier (%)</th>
                             <th>Effective Date offset</th>
                             <th>Funding Spread (bps)</th>
                             <th>Observation Frequency (m)</th>
                             <th>OTC</th>
                           </tr>
                         </thead>
                         <tbody>
                           <tr>
                             <td>FCN</td>
                             <td>USD</td>
                             <td>AMD UW</td>
                             <td>NVDA UW</td>
                             <td></td>
                             <td></td>
                             <td></td>
                             <td></td>
                             <td>90%</td>
                             <td>Daily Memory</td>
                             <td>105%</td>
                             <td>10.00%</td>
                             <td></td>
                             <td>4</td>
                             <td>EKI</td>
                             <td>60%</td>
                             <td>5</td>
                             <td>10</td>
                             <td>1</td>
                             <td>OTC</td>
                           </tr>
                       
                           <tr>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                           </tr>
                       
                           <tr>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                             <td></td><td></td><td></td><td></td><td></td>
                           </tr>
                       
                         </tbody>
                       </table>
                       """,
            Priority = "High",
            RequestReadReceipt = false,
            MessageId = $"AuotTicket-louisding70121@gmail.com-{Guid.NewGuid()}"
        };

        mail.From.Add(new EmailAddressModel
        {
            Name = "Louis",
            Address = "louisding70121@gmail.com"
        });

        mail.To.Add(new EmailAddressModel
        {
            Name = "LouisA",
            Address = "louistpdrgtesta@gmail.com"
        });
        
        var mailFun = new MailFun(_accountInfo, _mailSettings);
        mailFun.SendMail(mail);

        #endregion

        #region 驗證

        //待做

        #endregion
        
    }
    
    //測試顯示郵件線程功能
    [Test]
    public void DisplayThreadTest()
    {
        var mailFun = new MailFun(_accountInfo, _mailSettings);
        mailFun.GetDisplayThread();
    }

    //測試顯示郵件線程功能
    [Test]
    public void DisplayThreadTest2()
    {
        var accountInfo = new AccountInfo
        {
            Username = "louistpdrgtesta@gmail.com",
            Password = "ymoszrvjzzvthxcu"
        };
        _accountInfo = accountInfo;
        var mailFun = new MailFun(_accountInfo, _mailSettings);
        mailFun.GetDisplayThread();
    }
    
    
    [Test]
    public void FindMailTest()
    {
        var accountInfo = new AccountInfo
        {
            Username = "louistpdrgtesta@gmail.com",
            Password = "ymoszrvjzzvthxcu"
        };
        _accountInfo = accountInfo;
        var mailFun = new MailFun(_accountInfo, _mailSettings);
        var message =mailFun.GetMailBySenderAndMessageIdPrefix($"louisding70121@gmail.com",$"AuotTicket-louisding70121@gmail.com-060bdd36-3a02-42f5-96ef-e3df72195c62");
        
        if (message != null)
        {
            Console.WriteLine("Subject: " + message.Subject);
            Console.WriteLine("From: " + message.From);
            Console.WriteLine("Date: " + message.Date);
            Console.WriteLine("Body: " + message.Body);
        }
    }
}