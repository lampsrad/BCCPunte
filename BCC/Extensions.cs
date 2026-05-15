using BCC.Models;
using BCC.Viewmodels;
using System.Text.RegularExpressions;

namespace BCC
{
    public static class Extensions
    {
        public static IEnumerable<T> toEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public static int? AddNull(this int? x, int? y) //Adds two nullable integers
        {
            Int32? total;
            x = x ?? 0;
            y = y ?? 0;
            total = x + y;
            return total = total == 0 ? null : total;
        }
        public static string toCap(this string text)
        {
            string result = Regex.Replace(text, @"\b\w+", match =>
            {
                string word = match.Value;
                return char.ToUpper(word[0]) + word.Substring(1).ToLower();
            });
            return result;
        }
        public static string toFileName(this string dirFn)
        {
            return dirFn.Substring(dirFn.LastIndexOf("\\") + 1);
        }
        public static bool IsNumber(this string str)
        {
            float f;
            return float.TryParse(str, out f);
        }
        public static string toAward(this string? aw)
        {
            string award = aw.ToUpper() switch
            {
                "C" => "Com",
                "G" => "Gold",
                "S" => "Silver",
                "B" => "Bronz",
                _ => string.Empty
            };
            return award;
        }
        public static string toCategory_Rating(this string cat)
        {
            string catout = cat switch
            {
                "N4" => "Nature - Honours & Galaxy",
                "N3" => "Nature - 4 & 5 Star",
                "N2" => "Nature - 3 Star",
                "N1" => "Nature - 1 & 2 Star",
                "P4" => "Pictorial - Honours & Galaxy",
                "P3" => "Pictorial - 4 & 5 Star",
                "P2" => "Pictorial - 3 Star",
                "P1" => "Pictorial - 1 & 2 Star",
                "S" => "Set Subject",
                "PH" => "Cell-Phone",
                _ => null
            };
            return catout;
        }
        public static DateOnly toClubDate(this DateOnly date)
        {
            DateOnly dat = date.Month < 11 ? DateOnly.Parse((date.Year - 1).ToString() + "-11-01") : DateOnly.Parse(date.Year.ToString() + "-11-01");
            return dat;
        }
        public static DateOnly? toDate(this string datestr)
        {
            if (DateOnly.TryParse(datestr, out DateOnly date))
                return date;
            else
                return null;
        }
        public static DateOnly toDateFirstDay(this DateOnly date)
        {
            DateOnly datefirst = DateOnly.Parse($"{date.Year}-{date.Month}-{01}");
            return datefirst;
        }
        public static string toStarGroup(this int rating)
        {
            string outstr = rating switch
            {
                1 or 2 => "1 & 2 Star",
                3 => "3 Star",
                4 or 5 => "4 & 5 Star",
                6 or 7 => "Golden Honours and Galaxy",
                _ => string.Empty
            };
            return outstr;
        }
        public static string toYearMonthDirectory(this DateOnly date)
        {
            string month = date.Month < 10 ? "0" + date.Month.ToString() : date.Month.ToString();
            string datstr = date.Year.ToString() + "-" + month + @"\";

            return datstr;
        }
        public static string toYearMonthRoot(this DateOnly date)
        {
            string month = date.Month < 10 ? "0" + date.Month.ToString() : date.Month.ToString();
            string datstr = "/images/" + date.Year.ToString() + "-" + month + "/";

            return datstr;
        }
        public static int toMonthDigit(this string month)
        {
            var m1 = Regex.Match(month, @"\w{3}");
            string mon = m1.Value.ToLower();
            int m = mon switch
            {
                "jan" => 1,
                "feb" => 2,
                "maa" or "mar" => 3,
                "apr" => 4,
                "mei" or "may" => 5,
                "jun" => 6,
                "jul" => 7,
                "aug" => 8,
                "sep" => 9,
                "okt" or "oct" => 10,
                "nov" => 11,
                "des" or "dec" => 12,
                _ => 0
            };
            return m;
        }
        public static string toMonthFull_Year(this DateOnly date)
        {
            month m;
            m = (month)date.Month;
            string monthstr = m.ToString() + " " + date.Year.ToString();
            return monthstr;
        }
        public static string toMonthShort_Year(this DateOnly date)
        {
            monthS m;
            m = (monthS)date.Month;
            return m.ToString() + " " + date.Year;
        }
        public static string toYear_MonthLowerFull(this DateOnly date)
        {
            monthL m;
            m = (monthL)date.Month;
            string monthstr = date.Year.ToString() + " " + m.ToString();
            return monthstr;
        }
        public static string toMonth_YearFull(this DateOnly date)
        {
            month m;
            m = (month)date.Month;
            string outstr = m.ToString() + " " + date.Year.ToString();
            return outstr;
        }
        public static string toMonth(this DateOnly date)
        {
            month m;
            m = (month)date.Month;
            return m.ToString();
        }
        public static string toMonthShort(this DateOnly date)
        {
            monthS m;
            m = (monthS)date.Month;
            return m.ToString();
        }
        public static string toYear_Month_Digits(this DateOnly date)
        {
            string monthstr = date.Month < 10 ? "0" + date.Month.ToString() : date.Month.ToString();
            string outstr = date.Year.ToString() + "-" + monthstr;
            return outstr;
        }
        public static string toYearMonth_Tag(this DateOnly date)
        {
            string monthstr = date.Month < 10 ? "0" + date.Month.ToString() : date.Month.ToString();
            string outstr = date.Year.ToString() + monthstr;
            return outstr;
        }
        public static ParticipantsVM toParticipantsVM(this Photo phot)
        {
            ParticipantsVM vm = new ParticipantsVM();
            vm.ID = phot.ID;
            vm.Fullname = phot.Monthly.Master.Name;
            vm.CategoryStarGroup = phot.Category.toCategory_Rating();
            vm.ClubWinner = phot.Club_Winner == true ? "Win" : null;
            vm.Winner = phot.Winner == true ? "Win" : null;
            vm.Score = phot.Score;
            vm.Award = phot.Award.toAward();
            return vm;
        }
        public static IList<ParticipantsVM> toVMParticipants(this IEnumerable<Photo> photos)
        {
            IList<ParticipantsVM> vms = new List<ParticipantsVM>();
            foreach (Photo p in photos)
            {
                vms.Add(p.toParticipantsVM());
            }
            return vms;
        }
        public static Photo toPhotoView(this Photo p) 
        {
            DateOnly? date = p.Monthly.Date;
            if (p.IntRef < 10000000)
                p.Filename = $"ClubPhotos/{date.Value.toYear_Month_Digits()}/{p.IntRef}.jpg";
            else
                p.Filename = "images/Censored.jpg";
            return p;
        }
        public static IList<Photo> toPhotosView(this IEnumerable<Photo> photos, State state = null)
        {
            foreach (Photo p in photos)
            {
                DateOnly? date = p.Monthly.Date;
                if (p.IntRef < 10000000)
                    p.Filename = $"ClubPhotos/{date.Value.toYear_Month_Digits()}/{p.IntRef}.jpg";
                else
                    p.Filename = "images/Censored.jpg";
                p.Date = date.Value.toMonth_YearFull();
                p.Name = p.Monthly.Master.Name;
                if (state != null)
                    p.Flag = state.Member == "album" ? true : false;
            }
            return photos.ToList();
        }
        public static wallVM toWall(this Monthly mon)
        {
            wallVM w = new wallVM();
            w.ID = mon.ID;
            w.Name = $"{mon.Master.Firstname} {mon.Master.Lastname}";
            w.Year = mon.Date.Year;
            w.Star=mon.RatingID;
            w.Py = mon.Py;
            w.VOy = mon.VOy;
            w.Saly = mon.Saly;
            w.clubMaster = mon.Py + mon.VOy;
            w.grandMaster = mon.Py + mon.VOy + mon.Saly;
            return w;
        }
        public static IList<wallVM> toWallofFame(this IEnumerable<Monthly> monthlies)
        {
            IList<wallVM> vms = new List<wallVM>();
            foreach (var m in monthlies)
            {
                vms.Add(m.toWall());
            }
            return vms;
        }
    }
}
