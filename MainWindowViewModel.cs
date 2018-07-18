using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SweetHome
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private bool _busy;

        public bool Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Apartment> _apartments;

        public ObservableCollection<Apartment> Apartments
        {
            get { return _apartments; }
            set { _apartments = value;
                OnPropertyChanged();
            }
        }

        public ICommand ScrapCommand => new RelayCommand(ExecuteScrapCommand);
        public ICommand ValidateCommand => new RelayCommand(ExecuteValidateCommand);
        public ICommand OpenCommand => new RelayCommand(ExecuteOpenCommand);

        private List<string> hashes;

        private async void ExecuteScrapCommand(object param)
        {
            Busy = true;

            var ini = new IniFile("conf.ini");
            string link = ini.IniReadValue("Links", "SeLoger");
            if (!string.IsNullOrWhiteSpace(link))
                await ScrapApartmentsSeLoger(link);

            link = ini.IniReadValue("Links", "LogicImmo");
            if (!string.IsNullOrWhiteSpace(link))
            {
                await ScrapApartmentsLogicImmo(link);

                for (int i = 2; i < 9; i++)
                    await ScrapApartmentsLogicImmo(link.Replace("/pricemax", "/page=" + i + "/pricemax"));
            }
            //done
            link = ini.IniReadValue("Links", "Foncia");
            if (!string.IsNullOrWhiteSpace(link))
            {
                for (int i = 1; i < 3; i++)
                {
                    await ScrapApartmentsFoncia(link.Replace("page-1", "page-" + i));
                }
            }

            link = ini.IniReadValue("Links", "ParuVendu");
            if (!string.IsNullOrWhiteSpace(link))
            {
                for (int i = 1; i < 5; i++)
                {
                    await ScrapApartmentsParuVendu(link.Replace("&p=1", "&p=" + i));
                }
            }

            link = ini.IniReadValue("Links", "LeBonCoin");
            if (!string.IsNullOrWhiteSpace(link))
            {
                //https://www.leboncoin.fr/locations/offres/rhone_alpes/?o=1&mrs=400&mre=800&sqs=2&ros=2&ret=2&furn=1&location=Villeurbanne%2069100%2CLyon%2069003%2CLyon%2069006
                await ScrapApartmentsLeBonCoin(link);
            }
            Busy = false;
        }

        private async void ExecuteValidateCommand(object param)
        {
            string link = (string)param;
            var apar = Apartments.FirstOrDefault(x => x.Link == link);
            if (apar != null)
                Apartments.Remove(apar);
            File.AppendAllText("done.txt", CreateMD5(link) + Environment.NewLine);
        }

        private async void ExecuteOpenCommand(object param)
        {
            string link = (string)param;
            Process.Start(link);
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private HttpClient _client;
        public MainWindowViewModel()
        {
            Apartments = new ObservableCollection<Apartment>();
            _client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate });
            _client.DefaultRequestHeaders.ExpectContinue = false;
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.108 Safari/537.36");

            Busy = false;
            hashes = File.ReadAllLines("done.txt").ToList();
        }

        private Regex seLogerlinksRegex = new Regex("<a class=\"c-pa-link\\s*\\S*\" href=\"(?<link>\\S+)\"\\s*title=\".*\">.*<\\/a>");
        private Regex seLogerAdRegex = new Regex("<ul class=\"criterion\">\\s*(<li>(?<room>\\d*) pièces?<\\/li>)?\\s*(<li>(?<bedroom>\\d*) chambres?<\\/li>)?\\s*(<li>(?<area>\\d*,?\\d*) m²<\\/li>)?");
        private Regex seLogerPriceRegex = new Regex("<a href=\"#about_price_anchor\"\\s*class=\"js-smooth-scroll-link price\"\\s*data-smoothscroll-offset=\"\\d+\"\\s*>\\s*(?<price>\\d+) €\\s*<sup class=\"u-thin u-300 u-black-snow\">CC<\\/sup>\\s*<\\/a>");
        private Regex seLogerNextRegex = new Regex("href=\"(?<nextLink>\\S*)\">Suivant");
        private async Task ScrapApartmentsSeLoger(string url = "")
        {
            //seloger.com
            
            if (url == "")
                url = "http://www.seloger.com/list.htm?bedrooms=1&engine-version=new&picture=15&places=[{ci:690266%2bidq:121921,121920,121924,121925}],121926&price=NaN/1000&projects=1&qsversion=1.0&rooms=2,3,4,5&sort=a_surface&surface=26/NaN&types=1"; //http://www.seloger.com/list.htm?idtt=1&naturebien=1&idtypebien=1&ci=690266&tri=a_surface&si_meuble=1&nb_chambres=1,2&pxmax=1000&surfacemin=26";
            var resp = await _client.GetAsync(url);
            //http://www.seloger.com/list.htm?bedrooms=1&engine-version=new&picture=15&places=[{ci:690266%2bidq:121921,121920,121924,121925}],121926&price=NaN/1000&projects=1&qsversion=1.0&rooms=2,3,4,5&sort=a_surface&surface=26/NaN&types=1
            resp.EnsureSuccessStatusCode();
            var respData = await resp.Content.ReadAsStringAsync();
            var nextMatch = seLogerNextRegex.Match(respData);
            var matches = seLogerlinksRegex.Matches(respData);
            foreach (Match link in matches)
            {
                try
                {
                    if (hashes.Contains(CreateMD5(link.Groups["link"].Value)))
                        continue;
                    resp = await _client.GetAsync(link.Groups["link"].Value);
                    resp.EnsureSuccessStatusCode();
                    respData = await resp.Content.ReadAsStringAsync();
                    var match = seLogerAdRegex.Match(respData);
                    var priceMatch = seLogerPriceRegex.Match(respData);

                    if (match.Success && priceMatch.Success)
                    {
                        Apartments.Add(new Apartment()
                        {
                            Price = priceMatch.Groups["price"].Value,
                            Area = match.Groups["area"].Value,
                            Rooms = match.Groups["room"].Value,
                            Bedrooms = match.Groups["bedroom"].Value,
                            Link = link.Groups["link"].Value,

                        });
                    }
                }
                catch (Exception ex)
                {

                }
            }
            
            /*if (nextMatch.Success)
            {
                await ScrapApartmentsSeLoger("http:" + nextMatch.Groups["nextLink"].Value);
            }*/
        }

        private Regex logicImmoLinksRegex = new Regex("<a href=\"(?<link>.*)\" title=\".*\" class=\"offer-link\"");
        private Regex logicImmoAreaRegex = new Regex("<span class=\"offer-area-number\">(?<area>\\d+)<\\/span>");
        private Regex logicImmoPriceRegex = new Regex("<h2 class=\"main-price\">(?<price>\\d+) €<\\/h2>");
        private async Task ScrapApartmentsLogicImmo(string url = "")
        {
            var resp = await _client.GetAsync(url);
            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch
            {
                return;
            }
            var respData = await resp.Content.ReadAsStringAsync();
            //var nextMatch = seLogerNextRegex.Match(respData);
            var matches = logicImmoLinksRegex.Matches(respData);
            foreach (Match link in matches)
            {
                if (hashes.Contains(CreateMD5(link.Groups["link"].Value)))
                    continue;
                resp = await _client.GetAsync(link.Groups["link"].Value);
                try
                {
                    resp.EnsureSuccessStatusCode();
                }
                catch
                {
                    continue;
                }
                respData = await resp.Content.ReadAsStringAsync();
                var match = logicImmoAreaRegex.Match(respData);
                var priceMatch = logicImmoPriceRegex.Match(respData);

                Apartments.Add(new Apartment()
                {
                    Price = "0",
                    Area = "0",
                    Rooms = "0",// match.Groups["room"].Value,
                    Bedrooms = "0", //match.Groups["bedroom"].Value,
                    Link = link.Groups["link"].Value,

                });
                /*if (match.Success && priceMatch.Success)
                {
                    Apartments.Add(new Apartment()
                    {
                        Price = priceMatch.Groups["price"].Value,
                        Area = match.Groups["area"].Value,
                        Rooms = "0",// match.Groups["room"].Value,
                        Bedrooms = "0", //match.Groups["bedroom"].Value,
                        Link = link.Groups["link"].Value,

                    });
                }*/

            }
        }

        private Regex fonciaLinksRegex = new Regex("<h3 class=\"TeaserOffer-title\"><a href=\"(?<link>.*)\">");
        private Regex fonciaAreaRegex = new Regex("<p class=\"MiniData-item\">\\s*(?<area>.*)<sup>");
        private Regex fonciaPriceRegex = new Regex("<p class=\"OfferTop-price\">\\s*(?<price>.*)<sup>");
        private async Task ScrapApartmentsFoncia(string url)
        {
            var resp = await _client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var respData = await resp.Content.ReadAsStringAsync();
            var matches = fonciaLinksRegex.Matches(respData);
            foreach (Match link in matches)
            {
                resp = await _client.GetAsync("https://fr.foncia.com" + link.Groups["link"].Value);
                resp.EnsureSuccessStatusCode();
                respData = await resp.Content.ReadAsStringAsync();
                var match = fonciaAreaRegex.Match(respData);
                var priceMatch = fonciaPriceRegex.Match(respData);

                if (match.Success && priceMatch.Success && !hashes.Contains(CreateMD5(link.Groups["link"].Value)))
                {
                    Apartments.Add(new Apartment()
                    {
                        Price = priceMatch.Groups["price"].Value,
                        Area = match.Groups["area"].Value,
                        Rooms = "0",// match.Groups["room"].Value,
                        Bedrooms = "0", //match.Groups["bedroom"].Value,
                        Link = "https://fr.foncia.com" + link.Groups["link"].Value,

                    });
                }

            }
        }

        private Regex paruVenduLinksRegex = new Regex("<a href=\"(?<link>.*)\" title=\".*\">\\s*<div");
        private Regex paruVenduAreaRegex = new Regex("<strong>Surface :<\\/strong>\\s*(?<area>\\d*)m<");
        private Regex paruVenduPriceRegex = new Regex("<div id=\"autoprix\">\\s*(?<price>.*)\\s*<span>");
        private async Task ScrapApartmentsParuVendu(string url)
        {
            var resp = await _client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var respData = await resp.Content.ReadAsStringAsync();
            var matches = paruVenduLinksRegex.Matches(respData);
            foreach (Match link in matches)
            {
                if (hashes.Contains(CreateMD5("https://www.paruvendu.fr" + link.Groups["link"].Value)))
                    continue;
                resp = await _client.GetAsync("https://www.paruvendu.fr" + link.Groups["link"].Value);
                resp.EnsureSuccessStatusCode();
                respData = await resp.Content.ReadAsStringAsync();
                var match = paruVenduAreaRegex.Match(respData);
                var priceMatch = paruVenduPriceRegex.Match(respData);

                if (match.Success && priceMatch.Success)
                {
                    Apartments.Add(new Apartment()
                    {
                        Price = priceMatch.Groups["price"].Value,
                        Area = match.Groups["area"].Value,
                        Rooms = "0",// match.Groups["room"].Value,
                        Bedrooms = "0", //match.Groups["bedroom"].Value,
                        Link = "https://www.paruvendu.fr" + link.Groups["link"].Value,

                    });
                }

            }
        }
        //todo https://www.avendrealouer.fr/recherche.html?pageIndex=2&sortPropertyName=Price&sortDirection=Descending&searchTypeID=2&typeGroupCategoryID=6&transactionId=2&localityIds=101-33444&typeGroupIds=47&maximumPrice=800&minimumSurface=26&roomComfortIds=2,3,4,5&bedroomComfortIds=1,2,3,4,5


        private Regex leboncoinLinksRegex = new Regex("class=\"clearfix trackable\" href=\"(?<link>\\S*)\" data-reactid=\"\\d+\">");
            //new Regex("<li itemscope itemtype=\"http:\\/\\/schema\\.org\\/Offer\">\\s*<a href=\"(?<link>\\S*)\" title");
        private Regex leboncoinAreaRegex = new Regex("\"key\":\"square\",\"value\":\"(?<area>\\d+)\"");
        private Regex leboncoinPriceRegex = new Regex("\"price\":\\[(?<price>\\d+)\\]");
        private async Task ScrapApartmentsLeBonCoin(string url)
        {
            var resp = await _client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var respData = await resp.Content.ReadAsStringAsync();
            var matches = leboncoinLinksRegex.Matches(respData);
            foreach (Match link in matches)
            {
                if (hashes.Contains(CreateMD5("https://www.leboncoin.fr" + link.Groups["link"].Value)))
                    continue;
                resp = await _client.GetAsync("https://www.leboncoin.fr" + link.Groups["link"].Value);
                resp.EnsureSuccessStatusCode();
                respData = await resp.Content.ReadAsStringAsync();
                var match = leboncoinAreaRegex.Match(respData);
                var priceMatch = leboncoinPriceRegex.Match(respData);

                if (match.Success && priceMatch.Success)
                {
                    Apartments.Add(new Apartment()
                    {
                        Price = priceMatch.Groups["price"].Value,
                        Area = match.Groups["area"].Value,
                        Rooms = "0",// match.Groups["room"].Value,
                        Bedrooms = "0", //match.Groups["bedroom"].Value,
                        Link = "https://www.leboncoin.fr" + link.Groups["link"].Value,

                    });
                }

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
