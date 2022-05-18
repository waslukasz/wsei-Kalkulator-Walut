using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
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
        record Rate(string currency, string code, decimal ask, decimal bid);
        Dictionary<string, Rate> Rates = new Dictionary<string, Rate>();

        public MainWindow()
        {
            InitializeComponent();
            DownloadData();
            foreach (var code in Rates)
            {
                InputCurrencyCode.Items.Add(code.Key);
                OutputCurrencyCode.Items.Add(code.Key);
            }
            OutputCurrencyCode.SelectedIndex = 0;
            InputCurrencyCode.SelectedValue = "PLN";

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
                Rates.Add(rate.code, rate);
            }
            Rates.Add("PLN", new Rate("polski złoty", "PLN", 1, 1));
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            //pobrac kwote
            // pobrac kod waluty kwoty
            // pobrac kod waluty kwoty docelowej
            // obliczyc
            decimal input = Decimal.Parse(InputAmount.Text);
            Rates.TryGetValue(InputCurrencyCode.Text, out Rate inputRate);
            Rates.TryGetValue(OutputCurrencyCode.Text, out Rate outputRate);
            OutputAmount.Text = $"{input / outputRate.ask * inputRate.ask}";
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            string oldText = InputAmount.Text;
            string deltaText = e.Text;
            e.Handled = !decimal.TryParse(oldText + deltaText, out decimal val);
        }
    }
}
