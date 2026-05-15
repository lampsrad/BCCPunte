using BCC.Models;
using BCC.Viewmodels;

namespace BCC.Services;

//public class SendEmail
//{
//    private State _state;

//    public SendEmail(State state)
//    {
//        _state = state;
//    }
//    public async Task sendEmail(object obj)
//    {
//        System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient
//        {
//            Host = gData.host,
//            Port = gData.port,
//            EnableSsl = true,
//            Credentials = new System.Net.NetworkCredential(gData.username, gData.password)
//        };
//        System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
//        message.From = new System.Net.Mail.MailAddress(gData.from);
//        message.To.Add(gData.webmaster);
//        if (_state.Flickr)
//        {
//            message.To.Add(gData.president);
//            message.To.Add(gData.treasurer);
//            message.To.Add(gData.pro);//Geo is die Public Relatons Officer; Skakel-beampte
//        }
//        switch (obj.GetType().Name)
//        {
//            case "Member":
//                Member member = obj as Member;
//                message.Subject = "New Member at Bloemfontein Camera Club website has registered";
//                message.Body = $"Title : {member.Title}\nInitials : {member.Initials}\nFirstname : {member.Firstname}\nCallname : {member.Callname}\nLastname : {member.Lastname}\nID Number : {member.IDnumber}\n\nResidential Address\nStreet Address : {member.StreetRA}\nSuburb : {member.SuburbRA}\nCity : {member.CityRA}\nCode : {member.CodeRA}\n\nPostal Address\nPoBox : {member.StreetPA}\nCity : {member.CityPA}\nZip Code : {member.CodePA}\n\nMobile : {member.Mobile}\nHome phone : {member.Home}\nEmail : {member.Email}\nStargrading : {member.Stargrading}\nPSSA Member : {member.PssaMember}\nHonours : {member.Honors}\nDiamond Rating : {member.Diamond}\nMerits : {member.Merits}\n\nFamily Member\nTitle : {member.Title1}\nInitials : {member.Initials1}\nFirstname : {member.Firstname1}\nCallname : {member.Callname1}\nLastname : {member.Lastname1}\nID Number : {member.IDnumber1}";
//                break;
//            case "VisitorVM":
//                VisitorVM visitor = obj as VisitorVM;
//                message.Subject = "Vistor at BCC website wants to visit us.";
//                message.Body = "Name : " + visitor.Firstname + " " + visitor.Lastname + '\n' + " Email : " + visitor.Email + '\n' + " Month for Visiit : " + visitor.Month + '\n' + " Message : " + visitor.Message;
//                break;
//            case "Pdf":
//                Pdf pdf = obj as Pdf;
//                message.Subject = "New Member at BCC website wants to register";
//                message.Body = $"The potential member downloaded the Application form with the following instructions : \nTo fill in the form.\nEmail the completed form to Chris De Wet.\nThe public relations officer of the club will contact him-her in due time.\n\nName : {pdf.Name}\nEmail : {pdf.Email}\nMobile : {pdf.Mobile}";
//                break;
//        }
//        await client.SendMailAsync(message);
//    }
//}
