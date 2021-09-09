using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace Loteria
{
    public class LotoEntity
    {
        public string ImageUrl { get; set; }
        public string SaleDescription { get; set; }
        public DateTime Date { get; set; }

        public IEnumerable<LotoNumber> Numbers { get; set; }
    }

    public class LotoNumber
    {
        public int Position { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
    }

    class Program
    {
        static ScrapingBrowser _browser = new ScrapingBrowser();

        static void Main(string[] args)
        {
            var pageAsHtml = GetHtml("https://leidsa.com");

            var allRawLotos = pageAsHtml.OwnerDocument.DocumentNode.SelectNodes("//html/body/div/section/div/div/div");
            var lotoEntity = GetLoto(allRawLotos.ElementAtOrDefault(0));

            var kinoTvEntity = GetKinoTV(allRawLotos.ElementAtOrDefault(2));
        }

        static HtmlNode GetHtml(string url)
        {
            WebPage webpage = _browser.NavigateToPage(new Uri(url));
            return webpage.Html;
        }

        static LotoEntity GetLoto(HtmlNode rawLotoBox)
        {
            var lotoBox = rawLotoBox;
            if (lotoBox == null)
                return null;

            var imageUrl = lotoBox.SelectSingleNode("img")?.GetAttributeValue("src")?.CleanInnerText();

            var descriptionList = lotoBox?.SelectNodes("div/p") ?? Enumerable.Empty<HtmlNode>();
            var saleDescription = descriptionList.FirstOrDefault(c => c.HasClass("txt-dias-ventas"))?.InnerHtml?.Replace("<br>", ". ")?.CleanInnerText();

            var dateAsRawString = descriptionList.FirstOrDefault(c => c.HasClass("resultados-del-dia"))?.InnerText?.Replace("Resultados del ", " ")?.CleanInnerText();
            DateTime.TryParseExact(dateAsRawString, "dd 'de' MMMM 'del' yyyy", CultureInfo.CreateSpecificCulture("es"), DateTimeStyles.None, out var date);

            return new LotoEntity
            {
                ImageUrl = imageUrl,
                SaleDescription = saleDescription,
                Date = date,
                Numbers = GetLotoNumbers(lotoBox)
            };
        }

        static IEnumerable<LotoNumber> GetLotoNumbers(HtmlNode lotoBox)
        {
            var numberList = new List<LotoNumber>();
            var firstLotoNumbersBox = lotoBox?.SelectSingleNode("div/table/tbody");
            var rawNumbers = firstLotoNumbersBox?.SelectNodes("tr/td").ToList() ?? Enumerable.Empty<HtmlNode>();
            foreach (var rawNumber in rawNumbers)
            {
                var numberValue = Regex.Match(
                    rawNumber.InnerText?.CleanInnerText(),
                    @"\d+"
                )?.ToString();
                var numberPosition = numberList.Count;
                numberList.Add(new LotoNumber
                {
                    Value = numberValue,
                    Position = numberPosition
                });
            }
            return numberList;
        }

        static LotoEntity GetKinoTV(HtmlNode rawLotoBox)
        {
            var lotoBox = rawLotoBox?.SelectSingleNode("div");
            if (lotoBox == null)
                return null;

            var panelHeading = lotoBox.SelectSingleNode("div");
            var imageUrl = panelHeading?.SelectSingleNode("img")?.GetAttributeValue("src")?.CleanInnerText();

            var dateAsRawString = panelHeading?.SelectSingleNode("p")?.InnerText?.CleanInnerText()?.Replace("Sorteo: ", "")?.Replace(".", "");
            DateTime.TryParseExact(dateAsRawString, "dd 'de' MMMM 'del' yyyy", CultureInfo.CreateSpecificCulture("es"), DateTimeStyles.None, out var date);

            var saleDescription = lotoBox?.CssSelect("div.numeros-sorteos")?.CssSelect("p.txt-ventas-dias")?.FirstOrDefault()?.InnerText?.CleanInnerText();
            return new LotoEntity
            {
                ImageUrl = imageUrl,
                SaleDescription = saleDescription,
                Date = date,
                Numbers = GetLotoNumbers(lotoBox)
            };
        }
    }
}
