using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Brer.Attributes;
using Brer.Publisher.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCListenerHostedService.Models;

namespace MVCListenerHostedService.Controllers
{
    [EventListener]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBrerPublisher _brerPublisher;
        private readonly ITestInjection _testInjection;

        public HomeController(ILogger<HomeController> logger, IBrerPublisher brerPublisher,
            ITestInjection testInjection)
        {
            _logger = logger;
            _brerPublisher = brerPublisher;
            _testInjection = testInjection;
        }

        public IActionResult Index()
        {
            string topic = "MVM.Klantbeheer.KlantGeregistreerd";
            var evt = new KlantGeregistreerdEvent
            {
                KlantNummer = 101,
                KlantNaam = "Karina van Irak",
            };

            _brerPublisher.Publish(topic, evt);
            return View();
        }

        [Handler(topic: "MVM.Klantbeheer.KlantGeregistreerd")]
        public async Task Handle(KlantGeregistreerdEvent evt)
        {
            Console.WriteLine(_testInjection.TestInjectionString());
            Console.WriteLine("KlantGeregistreerdEvent event ontvangen!");
            Console.WriteLine($"\t - KlantNummer: {evt.KlantNummer}");
            throw new AggregateException();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }


    public class KlantGeregistreerdEvent
    {
        public int KlantNummer { get; set; }
        public string KlantNaam { get; set; }
    }
}
