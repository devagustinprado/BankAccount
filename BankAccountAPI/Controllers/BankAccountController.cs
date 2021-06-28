using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BankAccountAPI.Models;
using System.Buffers.Text;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;

namespace BankAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountController : ControllerBase
    {
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public BankAccountController(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        // GET: api/BankAccount
        [HttpGet]
        public IEnumerable<string> Get()
        {
            

            return new string[] { "value1", "value2" };
        }

        // GET: api/BankAccount/Santander/12345678/usuario/1234
        [HttpGet("Santander/{dni}/{user}/{pass}", Name = "Get")]
        public IActionResult GetSantander(string dni, string user, string pass)
        {
            #region Development overriding credentials
            if (_env.IsDevelopment())
            {
                GetDevCredentials(out dni, out user, out pass);
            }

            #endregion

            var ActionResult = Operate(ref dni, ref user, ref pass, out ChromeDriver driver);

            driver.Close();
            driver.Dispose(); 

            return ActionResult;
        }

        private IActionResult Operate(ref string dni, ref string user, ref string pass, out ChromeDriver driver)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            #region Load Page
            driver = new ChromeDriver()
            {
                Url = "https://www2.personas.santander.com.ar/obp-webapp/angular/#!/login"
            };
            //Thread.Sleep(5000);
            #endregion


            #region Decode credentials
            try
            {
                DecodeCredentials(ref dni, ref user, ref pass);
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                return BadRequest($"Failed to decode credentials. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
            }
            #endregion

            #region Login
            try
            {
                Login(dni, user, pass, driver);
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                return Unauthorized($"Failed to login. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
            }
            #endregion


            // TO-DO: Replace with a wait functionality.
            Thread.Sleep(6000);

            #region Get BankAccount Information
            BankAccountResponse bankAccountResponse;
            try
            {
                bankAccountResponse = GetBankAccountInformation(driver);
            }
            catch (Exception e)
            {
                Logout(driver);
                stopwatch.Stop();
                return BadRequest($"Failed to get account information. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
            }
            #endregion


            #region Logout
            try
            {
                Logout(driver);
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                return BadRequest($"Failed to logout. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
            }
            #endregion

            stopwatch.Stop();
            bankAccountResponse.CallDuration = stopwatch.ElapsedMilliseconds;
            return Ok(bankAccountResponse);
        }

        private static BankAccountResponse GetBankAccountInformation(ChromeDriver driver)
        {
            BankAccountResponse bankAccountResponse;
            List<BankAccount> bankAccounts = new List<BankAccount>();

            IWebElement bankAccountPesosAmountElement = driver.FindElement(By.XPath("//*[@id=\"main-view\"]/home/div/div[3]/home-cards/div/md-content/home-card[1]/md-card/div/home-card-cuenta/md-card-title/md-card-title-text/div/div[2]/div[1]/div/obp-formato-numero[1]"));
            decimal bankAccountPesos = Convert.ToDecimal(bankAccountPesosAmountElement.Text.Split(" ")[2].Replace("\r\n", "").Trim());

            IWebElement bankAccountDolarAmountElement = driver.FindElement(By.XPath("//*[@id=\"main-view\"]/home/div/div[3]/home-cards/div/md-content/home-card[1]/md-card/div/home-card-cuenta/md-card-title/md-card-title-text/div/div[2]/div[1]/div/obp-formato-numero[2]"));
            decimal bankAccountDolar = Convert.ToDecimal(bankAccountDolarAmountElement.Text.Split(" ")[2].Replace("\r\n", "").Trim());

            bankAccounts.Add(new BankAccount()
            {
                PesosAmount = bankAccountPesos,
                DolarAmount = bankAccountDolar
            });


            IWebElement cardsElement = driver.FindElement(By.XPath($"//*[@id=\"main-view\"]/home/div/div[3]/home-cards/div/md-content"), 4);
            int homeCardCuentaCount = cardsElement.FindElements(By.TagName("home-card-cuenta")).Count;

            List<Card> cards = new List<Card>();

            for (int i = 1; i < homeCardCuentaCount+1; i++)
            {
                if (driver.FindElement(By.XPath($"//*[@id=\"main-view\"]/home/div/div[3]/home-cards/div/md-content/home-card[{i}]/md-card/div/home-card-cuenta"), 8)
                    .GetAttribute("data-ng-if").Equals("$ctrl.cajaData.isTarjeta"))
                {
                    IWebElement cardElement = driver.FindElement(By.XPath($"//*[@id=\"main-view\"]/home/div/div[3]/home-cards/div/md-content/home-card[{i}]/md-card/div/home-card-cuenta/md-card-title"), 8);
                    IWebElement cardTypeElement = cardElement.FindElement(By.XPath($".//md-card-title-text/div/div[1]/span"));
                    string cardType = cardTypeElement.Text.Split(" ")[0].Trim();

                    // click on card box
                    cardElement.Click();

                    //Thread.Sleep(4000);

                    // click on "Último Resumen" tab
                    IWebElement lastStatementElement = driver.FindElement(By.XPath("//*[@id=\"main-view\"]/tarjetas/div/md-content/md-nav-bar/div/nav/ul/li[2]/button"), 8);
                    lastStatementElement.Click();

                    Thread.Sleep(1500); // due date it's not loaded at time.
                    IWebElement cardDueDateElement = driver.FindElementNotEmpty(By.XPath("//*[@id=\"main-view\"]/tarjetas/div/div/tarjeta-ultimo-resumen/div[2]/div/div[1]/h3[2]/span[2]"), 4);
                    DateTime cardDueDate = Convert.ToDateTime(cardDueDateElement.Text.Trim());

                    IWebElement cardPesosAmountElement = driver.FindElementNotEmpty(By.XPath("//*[@id=\"main-view\"]/tarjetas/div/div/tarjeta-detalle/section/div[1]/span[2]/obp-formato-numero"), 4);
                    decimal cardPesosAmount = Convert.ToDecimal(cardPesosAmountElement.Text.Split(" ")[2].Replace("\r\n", "").Trim());

                    IWebElement cardDolarAmountElement = driver.FindElementNotEmpty(By.XPath("//*[@id=\"main-view\"]/tarjetas/div/div/tarjeta-detalle/section/div[2]/span[2]/obp-formato-numero"), 4);
                    decimal cardDolarAmount = Convert.ToDecimal(cardDolarAmountElement.Text.Split(" ")[2].Replace("\r\n", "").Trim());

                    cards.Add(new Card()
                    {
                        PesosAmount = cardPesosAmount,
                        DolarAmount = cardDolarAmount,
                        Type = cardType,
                        DueDate = cardDueDate
                    });

                    driver.Navigate().Back();
                }
            }

            bankAccountResponse = new BankAccountResponse()
            {
                Bank = "Santander",
                BankAccounts = bankAccounts,
                Cards = cards
            };
            return bankAccountResponse;
        }

        private static void Logout(ChromeDriver driver)
        {
            IWebElement closeElement = driver.FindElement(By.XPath("//*[@id=\"topbar\"]/div[1]/div/div[3]/a[4]"));
            closeElement.Click();

            //Thread.Sleep(1500);

            IWebElement closeYesElement = driver.FindElement(By.XPath("/html/body/div[2]/md-dialog/topbar-logout-dialog/div/md-dialog-actions/div[2]/obp-boton/button"), 4);
            closeYesElement.Click();

            Thread.Sleep(1500);
        }

        private static void Login(string dni, string user, string pass, ChromeDriver driver)
        {
            IWebElement dniElement = driver.FindElement(By.Id("input_0"), 2);
            dniElement.SendKeys(dni);
            IWebElement passElement = driver.FindElement(By.Id("input_1"), 2);
            passElement.SendKeys(pass);
            IWebElement userElement = driver.FindElement(By.Id("input_2"), 2);
            userElement.SendKeys(user);

            //Thread.Sleep(500);

            IWebElement loginElement = driver.FindElement(By.XPath("//*[@id=\"form\"]/button"), 3);
            loginElement.Click();
        }

        private void GetDevCredentials(out string dni, out string user, out string pass)
        {
            dni = _configuration.GetValue<string>("SantanderDNI");
            user = _configuration.GetValue<string>("SantanderUser");
            pass = _configuration.GetValue<string>("SantanderPass");
        }

        private static void DecodeCredentials(ref string dni, ref string user, ref string pass)
        {
            byte[] dniBytes = Base64UrlTextEncoder.Decode(dni);
            byte[] userBytes = Base64UrlTextEncoder.Decode(user);
            byte[] passBytes = Base64UrlTextEncoder.Decode(pass);

            dni = Encoding.UTF8.GetString(dniBytes, 0, dniBytes.Length);
            user = Encoding.UTF8.GetString(userBytes, 0, userBytes.Length);
            pass = Encoding.UTF8.GetString(passBytes, 0, passBytes.Length);
        }

        // POST: api/BankAccount
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/BankAccount/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
