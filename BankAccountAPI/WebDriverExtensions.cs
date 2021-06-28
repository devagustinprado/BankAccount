using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankAccountAPI
{
    public static class WebDriverExtensions
    {
        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(500), 
                };
                return wait.Until(ExpectedConditions.ElementToBeClickable(by));
            }
            return driver.FindElement(by);
        }

        public static IWebElement FindElementNotEmpty(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(500),
                };
                return wait.Until(el => el.FindElements(by).Where(x => !string.IsNullOrWhiteSpace(x.Text)).FirstOrDefault());
            }
            return driver.FindElement(by);
        }
    }
}
