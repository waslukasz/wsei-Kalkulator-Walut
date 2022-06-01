using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Aplikacja
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        record Rate
        {
            [JsonPropertyName("currency")]
            public string Currency { get; set; }
            [JsonPropertyName("code")]
            public string Code { get; set; }
            [JsonPropertyName("ask")]
            public decimal Ask { get; set; }
            [JsonPropertyName("bid")]
            public decimal Bid { get; set; }

            public Rate(string Currency, string Code, decimal Ask, decimal Bid)
            {
                this.Currency = Currency;
                this.Code = Code;
                this.Ask = Ask;
                this.Bid = Bid;
            }

            public Rate() { }
        };
        Dictionary<string, Rate> Rates = new Dictionary<string, Rate>();

        public MainWindow()
        {
            InitializeComponent();
            Task.Run(() =>
            {
                try
                {
                    DownloadJsonData();
                    Application.Current.Dispatcher.Invoke(() => UpdateGui());
                    CalcBtn.Dispatcher.Invoke(() => CalcBtn.IsEnabled = true);
                }
                catch (WebException ex)
                {
                    MessageBox.Show("Nieudana próba połączenia z API.", "Brak dostępu do sieci.");
                    CalcBtn.Dispatcher.Invoke(() => CalcBtn.IsEnabled = false);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show("Błąd formatu danych!", "Problem z wczytywaniem pliku Json.");
                    CalcBtn.Dispatcher.Invoke(() => CalcBtn.IsEnabled = false);
                }
            });


        }
        void UpdateGui()
        {
            InputCurrencyCode.Items.Clear();
            OutputCurrencyCode.Items.Clear();
            foreach (var code in Rates)
            {
                InputCurrencyCode.Items.Add(code.Key);
                OutputCurrencyCode.Items.Add(code.Key);
            }
            OutputCurrencyCode.SelectedIndex = 0;
            InputCurrencyCode.SelectedIndex = 1;
        }

        class RatesTable
        {
            [JsonPropertyName("table")]
            public string Table { get; set; }
            [JsonPropertyName("no")]
            public string Number { get; set; }
            [JsonPropertyName("tradingDate")]
            public DateTime TradingDate { get; set; }
            [JsonPropertyName("effectiveDate")]
            public DateTime EffectiveDate { get; set; }
            [JsonPropertyName("rates")]
            public List<Rate> Rates { get; set; }
        }

        private void DownloadJsonData()
        {
            CultureInfo info = CultureInfo.CreateSpecificCulture("en-EN");
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/json");
            string json = client.DownloadString("http://api.nbp.pl/api/exchangerates/tables/C/");
            var tableRates = JsonSerializer.Deserialize<List<RatesTable>>(json);
            RatesTable table = tableRates[0];
            table.Rates.Add(new Rate("polski złoty", "PLN", 1, 1));

            foreach (var rate in table.Rates)
            {
                Rates.Add(rate.Code, rate);
            }
        }

        private void DownloadData()
        {
            CultureInfo info = CultureInfo.CreateSpecificCulture("en-EN");
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/xml");
            string xmlRates = client.DownloadString("http://api.nbp.pl/api/exchangerates/tables/C/");

            XDocument bankApi = XDocument.Parse(xmlRates);
            var rates = bankApi
                .Element("ArrayOfExchangeRatesTable")
                .Element("ExchangeRatesTable")
                .Elements("Rates")
                .Elements("Rate")
                .Select(x => new Rate(
                    x.Element("Currency").Value,
                    x.Element("Code").Value,
                    Decimal.Parse(x.Element("Bid").Value, info),
                    Decimal.Parse(x.Element("Ask").Value, info)
                ));
            foreach (Rate rate in rates)
            {
                Rates.Add(rate.Code, rate);
            }
            Rates.Add("PLN", new Rate("polski złoty", "PLN", 1, 1));
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            //pobrac kwote
            // pobrac kod waluty kwoty
            // pobrac kod waluty kwoty docelowej
            // obliczyc
            Rates.TryGetValue(InputCurrencyCode.Text, out Rate inputRate);
            Rates.TryGetValue(OutputCurrencyCode.Text, out Rate outputRate);

            if (decimal.TryParse(InputAmount.Text, out decimal amount))
            {
                decimal input = Decimal.Parse(InputAmount.Text);
                OutputAmount.Text = $"{input / outputRate.Ask * inputRate.Ask}";
            };
            
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Wybierz plik z notowaniami.";
            dialog.Filter = "Plik tekstowy (*.txt)|*.txt|Plik json (*.json)|*.json";
            dialog.ShowDialog();
            if (dialog.ShowDialog() == true)
            {
                if(File.Exists(dialog.FileName))
                {
                    if(dialog.FileName.EndsWith(".txt"))
                    {
                        string[] lines = File.ReadAllLines(dialog.FileName);
                        Rates.Clear();
                        foreach (var line in lines)
                        {
                            string[] token = line.Split(";");
                            string code = token[0];
                            string currency = token[1];
                            string askStr = token[2];
                            string bidStr = token[3];
                            if (decimal.TryParse(askStr, out decimal ask) && decimal.TryParse(bidStr, out decimal bid))
                            {
                                Rate rate = new Rate() { Code = code, Currency = currency, Ask = ask, Bid = bid };
                                Rates.Add(rate.Code, rate);
                            };
                        }
                    } else if (dialog.FileName.EndsWith(".json"))
                    {
                        string content = File.ReadAllText(dialog.FileName);
                        Rates.Clear();
                        Rates = JsonSerializer.Deserialize<Dictionary<string, Rate>>(content);
                    }
                    UpdateGui();
                }
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Wybierz miejsce zapisu";
            dialog.Filter = "Plik json (*.json)|*.json";
            dialog.ShowDialog();
            if (dialog.ShowDialog() == true) File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(Rates.Values));
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            string oldText = InputAmount.Text;
            string deltaText = e.Text;
            e.Handled = !decimal.TryParse(oldText + deltaText, out decimal val);
        }
    }
}
