using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Microsoft.Win32;
using SCTVObjects;
using System.Runtime.InteropServices;
using RefreshUtilities;

namespace SCTV
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1301:AvoidDuplicateAccelerators")]
    public partial class MainForm : Form
    {
        private bool loggedIn = false;
        public static string[] blockedTerms;
        public static string[] foundBlockedTerms;
        public static string[] foundBlockedSites;
        public static string blockedTermsPath = "config\\BlockedTerms.txt";
        public static string foundBlockedTermsPath = "config\\FoundBlockedTerms.txt";
        public static string[] blockedSites;
        public static string blockedSitesPath = "config\\BlockedSites.txt";
        public static string foundBlockedSitesPath = "config\\foundBlockedSites.txt";
        public static string loginInfoPath = "config\\LoginInfo.txt";
        public bool adminLock = false;//locks down browser until unlocked by a parent
        public int loggedInTime = 0;
        public bool checkForms = true;
        public bool MonitorActivity = false; //determines whether safesurf monitors page contents, forms, sites, etc...
        int loginMaxTime = 20;//20 minutes
        TabCtlEx tabControlEx = new TabCtlEx();

        bool showVolumeControl = false;
        bool showAddressBar = true;

        private DateTime startTime;
        private string userName;
        Timer keepRunning_tour_Timer = new Timer();
        Timer documentLoaded_tour_Timer = new Timer();
        Timer documentLoaded_tourList_Timer = new Timer();
        //ExtendedWebBrowser hideMeBrowser;
        ExtendedWebBrowser bitVideoBrowser;
        public ExtendedWebBrowser tourBrowser;
        int keepRunningTimerTicks = 0;
        string startTourUrlString = "";
        public string documentStringLoaded = "";
        int currentPageNumber = 0;
        int nextPageNumber = 0;
        bool tourIsRunning = false;
        ArrayList tourList = new ArrayList();
        int currentTourIndex = 1;
        string currentPageURL = "";
        string previousPageURL = "";
        RefreshUtilities.RefreshUtilities refreshUtilities;
        string documentString = "";
        bool enteredTheContest = false;
        bool foundPrize = false;
        bool foundEnterToWin = false;
        bool foundNewContest = false;
        int numberOfPrizesEntered = 0;
        int numberOfCashtravaganzaEntered = 0;
        int numberOfUnclaimedEntered = 0;
        bool foundSubmit = false;
        bool foundSweeps = false;
        bool foundRevealAll = false;
        bool clickedRevealAll = false;
        bool foundBetterLuckNextTime = false;
        List<string> users = new List<string>();
        bool switchingUsers = false;
        string currentUser = "";
        bool loggingIn = false;
        bool allDone = false;

        public bool LoggedIn
        {
            set
            {
                loggedIn = value;

                if (loggedIn)
                {
                    UpdateLoginToolStripMenuItem.Visible = true;
                    parentalControlsToolStripMenuItem.Visible = true;
                    loginToolStripMenuItem.Visible = false;
                    logoutToolStripMenuItem.Visible = true;
                    logoutToolStripButton.Visible = true;
                    LoginToolStripButton.Visible = false;
                    adminToolStripButton.Visible = true;

                    loginTimer.Enabled = true;
                    loginTimer.Start();
                }
                else
                {
                    UpdateLoginToolStripMenuItem.Visible = false;
                    parentalControlsToolStripMenuItem.Visible = false;
                    loginToolStripMenuItem.Visible = true;
                    logoutToolStripMenuItem.Visible = false;
                    logoutToolStripButton.Visible = false;
                    LoginToolStripButton.Visible = true;
                    adminToolStripButton.Visible = false;
                    tcAdmin.Visible = false;

                    loginTimer.Enabled = false;
                    loginTimer.Stop();
                }
            }

            get
            {
                return loggedIn;
            }
        }

        public Uri URL
        {
            set { _windowManager.ActiveBrowser.Url = value; }
            get { return _windowManager.ActiveBrowser.Url; }
        }

        public bool ShowMenuStrip
        {
            set { this.menuStrip.Visible = value; }
        }

        public FormBorderStyle FormBorder
        {
            set { this.FormBorderStyle = value; }
        }

        public bool ShowLoginButton
        {
            set { LoginToolStripButton.Visible = value; }
        }

        public bool ShowJustinRecordButton
        {
            set { JustinRecordtoolStripButton.Visible = value; }
        }
        
        public bool ShowAddressBar
        {
            set { showAddressBar = value; }

            get { return showAddressBar; }
        }

        public string SetDocumentString
        {
            set
            {
                //documentLoaded_tour(value);
            }
        }

        public string SetTourListDocumentString
        {
            set
            {
                //documentLoaded_tourList(value);
            }
        }
        
        public MainForm()
        {
            InitializeComponent();

            try
            {
                useLatestIE();
                
                //keepRunning_tour_Timer.Enabled = true;
                //keepRunning_tour_Timer.Interval = 30000;//30 seconds
                //keepRunning_tour_Timer.Tick += KeepRunning_tour_Timer_Tick;
                //keepRunning_tour_Timer.Stop();

                //documentLoaded_tour_Timer.Enabled = true;
                //documentLoaded_tour_Timer.Interval = 10000;
                //documentLoaded_tour_Timer.Tick += DocumentLoaded_tour_Timer_Tick;
                //documentLoaded_tour_Timer.Stop();

                //documentLoaded_tourList_Timer.Enabled = true;
                //documentLoaded_tourList_Timer.Interval = 10000;
                //documentLoaded_tourList_Timer.Tick += DocumentLoaded_tourList_Timer_Tick;
                //documentLoaded_tourList_Timer.Stop();

                tabControlEx.Name = "tabControlEx";
                tabControlEx.SelectedIndex = 0;
                tabControlEx.Visible = false;
                tabControlEx.OnClose += new TabCtlEx.OnHeaderCloseDelegate(tabEx_OnClose);
                tabControlEx.VisibleChanged += new System.EventHandler(this.tabControlEx_VisibleChanged);

                this.panel1.Controls.Add(tabControlEx);
                tabControlEx.Dock = DockStyle.Fill;

                _windowManager = new WindowManager(tabControlEx);
                _windowManager.CommandStateChanged += new EventHandler<CommandStateEventArgs>(_windowManager_CommandStateChanged);
                _windowManager.StatusTextChanged += new EventHandler<TextChangedEventArgs>(_windowManager_StatusTextChanged);
                //_windowManager.DocumentCompleted += tour_DocumentCompleted;
                //_windowManager.ActiveBrowser.Navigating += ActiveBrowser_Navigating;
                //_windowManager.ActiveBrowser.ScriptErrorsSuppressed = true;
                _windowManager.ShowAddressBar = showAddressBar;
                
                showAddressBarToolStripMenuItem.Checked = showAddressBar;

                startTime = DateTime.Now;
                userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                initFormsConfigs();


                ////load blocked terms
                //loadBlockedTerms(blockedTermsPath);

                ////load blocked sites
                //loadBlockedSites(blockedSitesPath);

                ////load found blocked terms
                //loadFoundBlockedTerms(foundBlockedTermsPath);

                ////load found blocked sites
                //loadFoundBlockedSites(foundBlockedSitesPath);


                //getDefaultBrowser();
                
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);
                Application.Restart();
            }
        }

        //private void StartTourTimer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!tourIsRunning && startTourUrlString.Trim().Length > 0)
        //        {
        //            if (startTourUrlString.Length > 5)
        //            {
        //                tourIsRunning = true;
        //                tourList.RemoveAt(0);
        //                //tourBrowser.Url = new Uri(startTourUrlString);
        //            }
                    
        //            startTourUrlString = "";
        //            documentLoaded_tour_Timer.Stop();
        //            documentLoaded_tour_Timer.Tag = null;

        //            lblRefreshTimer.Text = "0 seconds";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Tools.WriteToFile(ex);

        //        Application.Restart();
        //    }
        //}

        //private void DocumentLoaded_tourList_Timer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        //documentLoaded_tourList_Timer.Stop();
        //        //documentLoaded_tourList_Timer.Tag = null;

        //        //bitVideoBrowser.Navigate("javascript: window.external.CallServerSideCode();");
        //    }
        //    catch (Exception ex)
        //    {
        //        //Application.Restart();
        //    }
        //}

        //private void DocumentLoaded_tour_Timer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        documentLoaded_tour_Timer.Stop();
        //        documentLoaded_tour_Timer.Tag = null;

        //        //if (tourBrowser != null && goToURLTimer.Tag == null)
        //        //{
        //        //    tourBrowser.Navigate("javascript: window.external.CallServerSideCode();");

        //        //    keepRunning_tour_Timer.Stop();
        //        //    keepRunning_tour_Timer.Tag = DateTime.Now;
        //        //    keepRunning_tour_Timer.Start();
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        //Application.Restart();
        //        string error = ex.Message;
        //    }            
        //}
        
        //private void KeepRunning_tour_Timer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (tourIsRunning)
        //        {
        //            keepRunningTimerTicks++;

        //            if (keepRunningTimerTicks >= 1)//30 seconds
        //            {
        //                //goToURLTimer.Stop();
        //                //goToURLTimer.Tag = null;
        //                documentLoaded_tourList_Timer.Stop();
        //                documentLoaded_tourList_Timer.Tag = null;
        //                documentLoaded_tour_Timer.Stop();
        //                documentLoaded_tour_Timer.Tag = null;
        //                //secondsTimer.Tag = null;
                        
        //                string currentPageString = tourBrowser.Url.ToString().Substring(0,tourBrowser.Url.ToString().Length - 1);
        //                currentPageString = currentPageString.Substring(currentPageString.LastIndexOf("/") + 1);
        //                int.TryParse(currentPageString, out nextPageNumber);
        //                nextPageNumber++;//add one to the current page number

        //                if (keepRunningTimerTicks < 3)
        //                {
        //                    string nextPageLink = tourBrowser.Url.ToString().Replace("/" + (nextPageNumber - 1).ToString() + "/", "/" + nextPageNumber + "/");

        //                    //if(nextPageLink != tourBrowser.Url.ToString())
        //                    //    goToURL(nextPageLink);
        //                }
        //                else
        //                {
        //                    tourBrowser.Refresh();
        //                    keepRunningTimerTicks = 0;
        //                }

        //                    //Application.Restart();
        //                    //tourBrowser.Refresh();//refresh page to get things going again
        //                    //keepRunning_tour_Timer.Stop();

        //                }
        //            //else if (keepRunningTimerTicks >= 7)//7 minutes
        //            //    Application.Restart();
        //        }
        //        else
        //        {
        //            keepRunning_tour_Timer.Stop();
        //            keepRunningTimerTicks = 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Tools.WriteToFile(ex);

        //        Application.Restart();
        //    }
        //}

        // Starting the app here...
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Open a new browser window

                //hideMeBrowser = _windowManager.New(false);
                //hideMeBrowser.Url = new Uri("https://us.hideproxy.me/index.php");



                bitVideoBrowser = this._windowManager.New();
                bitVideoBrowser.DocumentCompleted += BitVideoBrowser_DocumentCompleted;
                bitVideoBrowser.Url = new Uri("https://www.prizeloot.com/break/select_next");
                
                //bitVideoBrowser.StartNewWindow += BitVideoBrowser_StartNewWindow;

                try
                {
                    bitVideoBrowser.ObjectForScripting = new GetTourListDocumentString();
                }
                catch (Exception ex)
                {
                    throw;
                }

                refreshUtilities = new RefreshUtilities.RefreshUtilities();
                refreshUtilities.ClickComplete += RefreshUtilities_ClickComplete;
                refreshUtilities.GoToUrlComplete += RefreshUtilities_GoToUrlComplete;

                users.Add("lickey10@gmail.com|soccer");
                users.Add("lickeykids@gmail.com|soccer");
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(ex);
                //Application.Restart();
            }            
        }

        private void RefreshUtilities_GoToUrlComplete(object sender, EventArgs e)
        {
            if(sender != null && sender is RefreshUtilities.TimerInfo)
                bitVideoBrowser.Url = new Uri(((RefreshUtilities.TimerInfo)sender).UrlToGoTo);
        }

        private void RefreshUtilities_ClickComplete(object sender, EventArgs e)
        {
            if (!loggingIn)
            {
                //if the sender is the quick pick button then find and click the submit button
                //if (((HtmlElement)sender).InnerText != null && (((HtmlElement)sender).InnerText.Trim() == "QUICK PICK" || ((HtmlElement)sender).InnerText.Trim() == "AUTO PICK" || ((HtmlElement)sender).InnerText.Trim() == "PLAY LAST" || ((HtmlElement)sender).InnerText.Trim().Contains("USE FAVES") || ((HtmlElement)sender).InnerText.Trim().Contains("PLAY SAVED")))//this is the quick pick button - now click the submit button
                //{
                //    foundSubmit = findSubmit(bitVideoBrowser.Document);
                //    //foundEnterToWin = false;
                //}
                //else if (((HtmlElement)sender).InnerHtml != null && ((HtmlElement)sender).InnerHtml == "REVEAL ALL")
                //{
                //    clickedRevealAll = true;
                //    //foundEnterToWin = false;

                //    if (!foundBetterLuckNextTime && clickedRevealAll)
                //        foundBetterLuckNextTime = findBetterLuckNextTime(bitVideoBrowser.Document);

                //    if (!foundBetterLuckNextTime)
                //    {
                //        refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 5, true, lblRefreshTimer, bitVideoBrowser);

                //        //lblStatus.Text = "Looking for Better Luck Next Time";
                //    }
                //}
                //else if (((HtmlElement)sender).OuterHtml != null && ((HtmlElement)sender).OuterHtml.Contains("value=\"Secure My Entry\"")) //we entered a sweeps
                //{
                //    clickedRevealAll = false;
                //    //foundEnterToWin = false;

                //    numberOfPrizesEntered++;

                //    txtPrizeCount.Text = numberOfPrizesEntered.ToString();
                //}
                //else if (((HtmlElement)sender).InnerText != null && (((HtmlElement)sender).InnerText.Trim() == "SUBMIT" || ((HtmlElement)sender).InnerText.Trim() == "GO!"))//we clicked submit
                //{
                //    refreshUtilities.GoToURL("https://www.rewardit.com", true, lblRefreshTimer, bitVideoBrowser);
                //    //foundEnterToWin = false;

                //    numberOfPrizesEntered++;

                //    txtPrizeCount.Text = numberOfPrizesEntered.ToString();
                //}

                //if ((!foundSubmit && !clickedRevealAll) || bitVideoBrowser.Document.Url.ToString().ToLower().StartsWith("https://offers.rewardit.com") || bitVideoBrowser.Document.Url.ToString().ToLower().StartsWith("http://www.dealsprovidedtoyou.com"))
                //{
                //    foundEnterToWin = false;
                //    refreshUtilities.GoToURL("https://www.rewardit.com", 15, lblRefreshTimer, bitVideoBrowser);
                //}
            }
            //else
            //    populateUsernamePassword();
        }

        private void BitVideoBrowser_StartNewWindow(object sender, BrowserExtendedNavigatingEventArgs e)
        {
            if (tourBrowser == null || !tourBrowser.Created)
            {
                tourBrowser = this._windowManager.New();
                //tourBrowser.DocumentCompleted += tour_DocumentCompleted;
                tourBrowser.Downloading += TourBrowser_Downloading;
                tourBrowser.DownloadComplete += TourBrowser_DownloadComplete;
                tourBrowser.Navigating += TourBrowser_Navigating;
                tourBrowser.Url = e.Url;
                tourIsRunning = true;

                try
                {
                    tourBrowser.ObjectForScripting = new GetTourDocumentString();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        private void TourBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            lblDownloading.BackColor = Color.Green;
        }

        private void TourBrowser_DownloadComplete(object sender, EventArgs e)
        {
            lblStreaming.BackColor = Color.Red;
        }

        private void TourBrowser_Downloading(object sender, EventArgs e)
        {
            lblStreaming.BackColor = Color.Green;
        }

        private void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            //Application.Restart();
        }
        
        private void ActiveBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            //documentString = "";
        }

        private void BitVideoBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                documentString = _windowManager.ActiveBrowser.DocumentText;

                if (bitVideoBrowser.Url != null)
                {
                    if(documentString.Contains("<a href=\"/users/sign_in\">LOG IN</a>"))//need to login
                    {
                        if (!loggingIn && !allDone)
                        {
                            refreshUtilities.Cancel();
                            lblRefreshTimer.Text = "0 seconds";

                            login();
                        }
                    }
                    else if (bitVideoBrowser.Url.ToString().ToLower().Contains("https://www.prizeloot.com/break/select_next") || bitVideoBrowser.Url.ToString().ToLower() == "https://www.prizeloot.com/sweeps/")//this is the first page
                    {
                        loggingIn = false;

                        if (!foundNewContest)
                            findNextContestLink();
                        
                    }
                    else if (foundEnterToWin && bitVideoBrowser.Url.ToString().ToLower().Contains("/entries/"))//we have entered a contest and need to go back to the sweeps page
                    {
                        foundNewContest = false;
                        foundEnterToWin = false;
                        loggingIn = false;

                        refreshUtilities.GoToURL("https://www.prizeloot.com/sweeps/", 7, true, lblRefreshTimer, bitVideoBrowser);
                    }
                    else if (bitVideoBrowser.Url.ToString().ToLower().Contains("https://www.prizeloot.com/sweeps/") && bitVideoBrowser.Url.ToString().ToLower().Replace("https://www.prizeloot.com/sweeps/","").Length > 5)//we need to click the enter to win button
                    {
                        loggingIn = false;

                        if(!foundEnterToWin)
                            findEnterToWin();
                    }
                    
                    //else if (bitVideoBrowser.Url.Host != "www.rewardit.com")//we have left the site
                    //{
                    //    foundNewContest = false;
                    //    foundQuickPick = false;
                    //    foundSweeps = false;
                    //    foundRevealAll = false;
                    //    loggingIn = false;

                    //    refreshUtilities.GoToURL("https://www.rewardit.com", true, lblRefreshTimer, bitVideoBrowser);
                    //}
                    else if(bitVideoBrowser.Document.Url.ToString().ToLower().StartsWith("https://www.rewardit.com/site/userbux?") && !switchingUsers && !allDone)// we are done - switch users
                    {
                        switchUsers();
                    }
                    //else if (bitVideoBrowser.Url.ToString().ToLower().Contains("http://www.rewardit.com/dailydraw/") && foundQuickPick)//this is the last page
                    //{
                    //    findNextContestLink(bitVideoBrowser.DocumentText);
                    //}
                }
            }
            catch (Exception ex)
            {
                Application.Restart();
            }
        }
        
        private bool findEnterToWin()
        {
            if (!foundEnterToWin)
            {

                HtmlElementCollection elc = bitVideoBrowser.Document.GetElementsByTagName("a");
                //<a class="enter-btn" data-disable-with="<div class='pending-submit'><span class='inner-pending-text'>Processing Your Entry...</span></div>" rel="nofollow" data-method="post" href="/sweeps/1-000-cash-or-1-000-visa-gift-card/entries?entry%5Bprize_option%5D=1">
                      //< p class="prize-desc">$1,000 Cash</p>
                      //<p class="enter-to-win">Enter To Win</p>
                //</a>
                foreach (HtmlElement el in elc)
                {
                    if (el.OuterHtml != null && (el.OuterHtml.Contains("class='pending-submit'")))
                    {
                        refreshUtilities.ClickElement(el, true, lblRefreshTimer);
                        foundEnterToWin = true;
                        numberOfPrizesEntered++;
                        txtPrizeCount.Text = numberOfPrizesEntered.ToString();

                        return true;
                    }
                }
            }

            return false;
        }

        private bool findSubmit(HtmlDocument pageDocument)
        {
            HtmlElementCollection elc = pageDocument.GetElementsByTagName("button");

            foreach (HtmlElement el in elc)
            {
                if(el.InnerText != null && (el.InnerText.Trim() == "SUBMIT" || el.InnerText.Trim() == "GO!"))//this is the submit button - click it
                {
                    refreshUtilities.ClickElement(el, true, lblRefreshTimer);

                    return true;
                }
            }

            return false;
        }

        private bool findNextContestLink()
        {
            foundEnterToWin = false;

            //<a href="/sweeps/1-000-cash-or-1-000-visa-gift-card">

            //          < div class="text-container">
            //            <p class="prize-opt dt">$1,000 Visa Gift Card</p>
            //            <p class="prize-opt mob">$1,000 Visa GC</p>
            //            <p class="cash-opt">Or $1,000 Cash</p>
            //          </div>

            //          <div class="bottom-container">
            //            <!-- <div class="sweep-img-container">
            //            </div> -->
            //            <div class="entries-container">
            //              <p class="entry-period-count"><span class="count">0/5</span> Entries This Period</p>
            //            </div>

            //          </div>

            //            <p class="faux-link">ENTER NOW</p>
            //</a>

            HtmlElementCollection elc = bitVideoBrowser.Document.GetElementsByTagName("a");

            foreach (HtmlElement el in elc)
            {
                if (el.OuterHtml != null && el.OuterHtml.ToLower().Contains("href=\"/sweeps/"))//this is the next contest
                {
                    //get prizes left
                    string entries = findValue(el.InnerHtml, "class=\"count\">", "</span>");
                    int currentEntries = 0;
                    int totalEntries = 0;
                    
                    if(entries.Contains("/") && int.TryParse(entries.Split('/')[1], out totalEntries) && totalEntries > 0)
                    {
                        currentEntries = int.Parse(entries.Split('/')[0]);

                        if(currentEntries < totalEntries)
                        {
                            foundNewContest = true;

                            refreshUtilities.ClickElement(el, true, lblRefreshTimer);

                            return true;
                        }
                    }
                }
            }

            return false;

            //foundQuickPick = false;
            ////string nextContestLink = findValue(pageContent, "window.location='/DailyDraw/Index", "'");
            //string nextContestLink = findValue(pageContent, "window.location='/", "'");

            //if (nextContestLink.ToLower().Contains("/blog/"))
            //    foundQuickPick = true;

            //if (nextContestLink.Trim().Length == 0)
            //{
            //    nextContestLink = findValue(pageContent, "/Sweepstake/Index", "\"");

            //    if (nextContestLink.Trim().Length == 0)
            //        nextContestLink = "Sweepstake/Index" + nextContestLink;
            //}

            //if (nextContestLink.Trim().Length > 0)
            //{
            //    nextContestLink = bitVideoBrowser.Document.Url.Scheme +"://www.rewardit.com/" + nextContestLink;

            //    //if (nextContestLink.ToLower().Contains("/blog/"))
            //    //    foundNewContest = false;
            //    //else
            //        foundNewContest = true;

            //    refreshUtilities.GoToURL(bitVideoBrowser.Url.Scheme + "://"+ bitVideoBrowser.Url.Host + bitVideoBrowser.Url.Segments[0] + bitVideoBrowser.Url.Segments[1], true, lblRefreshTimer, bitVideoBrowser);
            //    //refreshUtilities.GoToURL(nextContestLink, lblRefreshTimer, bitVideoBrowser);

            //    return true;
            //}
            ////else
            ////    MessageBox.Show("didn't find any more contests");

            //return false;
        }

        private bool findSweeps(HtmlDocument pageDocument)
        {
            //look for email
            HtmlElementCollection elc = pageDocument.GetElementsByTagName("input");

            foreach (HtmlElement el in elc)
            {
                if (el.OuterHtml.Contains("type=\"email\""))//this is the email field
                {
                    el.SetAttribute("value", "lickey10@gmail.com");

                    break;
                }
            }

            //look for the submit button
            foreach (HtmlElement el in elc)
            {
                if (el.OuterHtml.Contains("value=\"Secure My Entry\""))//this is the submit button
                {
                    refreshUtilities.ClickElement(el, true, lblRefreshTimer);

                    foundSweeps = true;

                    return true;
                }
            }

            return false;
        }

        private bool findRevealAll(HtmlDocument pageDocument)
        {
            HtmlElementCollection elc = pageDocument.GetElementsByTagName("button");

            foreach (HtmlElement el in elc)
            {
                //if(el.OuterHtml != null && el.OuterHtml.Contains("class=\"hidden-xs partnersite-submit subSweep\"") && el.InnerText == null && el.InnerHtml == null)//this is the submit button - click it
                if (el.InnerHtml != null && el.InnerHtml == "REVEAL ALL")
                {
                    refreshUtilities.ClickElement(el, true, lblRefreshTimer);
                    //refreshCount = 0;

                    //lblStatus.Text = "Found Reveal All";

                    return true;
                }
            }

            return false;
        }

        private bool findBetterLuckNextTime(HtmlDocument pageDocument)
        {
            if (!foundBetterLuckNextTime)
            {
                HtmlElementCollection elc = pageDocument.GetElementsByTagName("a");

                foreach (HtmlElement el in elc)
                {
                    //<a href="/ScratchOff/Vehicle?SerialNumber=3c148f1f-c50f-44aa-8474-8a5006f24722" class="btn-block submit-button"><div class="text-uppercase congrats line1">Better Luck Next Time!</div><div class="congrats line2"> This was not a winning ticket.</div><div class="clearfix"></div><div class="congrats line2"><strong><em>Continue to your next sweepstakes...</em></strong></div></a>
                    if (el.GetAttribute("href").Contains("/ScratchOff/Vehicle?SerialNumber"))
                    {
                        refreshUtilities.ClickElement(el, true, lblRefreshTimer);
                        //foundMap = false;
                        //foundTryYourLuckAgain = false;
                        foundRevealAll = false;
                        //foundKeepTrying = false;
                        //foundLetsGo = false;

                        //lblStatus.Text = "Found Better Luck Next Time";

                        return true;
                    }
                }
            }

            return false;
        }

        private void initFormsConfigs()
        {
            SettingsHelper helper = SettingsHelper.Current;

            checkForms = helper.CheckForms;
        }

        private void useLatestIE()
        {
            try
            {
                string AppName = Application.ProductName;// My.Application.Info.AssemblyName
                int VersionCode = 0;
                string Version = "";
                object ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("svcUpdateVersion");

                if (ieVersion == null)
                    ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("Version");

                if (ieVersion != null)
                {
                    Version = ieVersion.ToString().Substring(0, ieVersion.ToString().IndexOf("."));
                    switch (Version)
                    {
                        case "7":
                            VersionCode = 7000;
                            break;
                        case "8":
                            VersionCode = 8888;
                            break;
                        case "9":
                            VersionCode = 9999;
                            break;
                        case "10":
                            VersionCode = 10001;
                            break;
                        default:
                            if (int.Parse(Version) >= 11)
                                VersionCode = 11001;
                            else
                                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: IE Version not supported");
                            break;
                    }
                }
                else
                {
                    Tools.WriteToFile(Tools.errorFile, "useLatestIE error: Registry error");
                }

                //'Check if the right emulation is set
                //'if not, Set Emulation to highest level possible on the user machine
                string Root = "HKEY_CURRENT_USER\\";
                string Key = "Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";
                
                object CurrentSetting = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key).GetValue(AppName + ".exe");

                if (CurrentSetting == null || int.Parse(CurrentSetting.ToString()) != VersionCode)
                {
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".exe", VersionCode);
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".vshost.exe", VersionCode);
                }
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: "+ ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        // Update the status text
        void _windowManager_StatusTextChanged(object sender, TextChangedEventArgs e)
        {
            this.toolStripStatusLabel.Text = e.Text;
        }

        // Enable / disable buttons
        void _windowManager_CommandStateChanged(object sender, CommandStateEventArgs e)
        {
            this.forwardToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Forward) == BrowserCommands.Forward);
            this.backToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Back) == BrowserCommands.Back);
            this.printPreviewToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printPreviewToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.printToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.homeToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Home) == BrowserCommands.Home);
            this.searchToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Search) == BrowserCommands.Search);
            this.refreshToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Reload) == BrowserCommands.Reload);
            this.stopToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Stop) == BrowserCommands.Stop);
        }

        #region Tools menu
        // Executed when the user clicks on Tools -> Options
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OptionsForm of = new OptionsForm())
            {
                of.ShowDialog(this);
            }
        }

        // Tools -> Show script errors
        private void scriptErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptErrorManager.Instance.ShowWindow();
        }

        //login to be able to access/modify blockedTerms file
        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.ShowDialog(this);
                if (login.DialogResult == DialogResult.Yes)
                {
                    LoggedIn = true;
                    adminLock = false;
                }
                else if (login.DialogResult == DialogResult.None)
                    adminLock = true;
                else
                    LoggedIn = false;
            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoggedIn = false;
        }

        private void UpdateLoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.Update = true;
                login.ShowDialog(this);
            }
        }

        private void modifyBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //display terms
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();

            tcAdmin.SelectedTab = tcAdmin.TabPages["tpChangeLoginInfo"];
        }

        private void modifyBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpBlockedSites"];
        }

        private void foundBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedTerms"];
        }

        private void foundBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedSites"];
        }
        #endregion

        #region File Menu

        // File -> Print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        // File -> Print Preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        // File -> Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // File -> Open URL
        private void openUrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenUrlForm ouf = new OpenUrlForm())
            {
                if (ouf.ShowDialog() == DialogResult.OK)
                {
                    ExtendedWebBrowser brw = _windowManager.New(false);
                    brw.Navigate(ouf.Url);
                }
            }
        }

        // File -> Open File
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = Properties.Resources.OpenFileDialogFilter;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Uri url = new Uri(ofd.FileName);
                    WindowManager.Open(url);
                }
            }
        }
        #endregion

        #region Help Menu

        // Executed when the user clicks on Help -> About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About();
        }

        /// <summary>
        /// Shows the AboutForm
        /// </summary>
        private void About()
        {
            using (AboutForm af = new AboutForm())
            {
                af.ShowDialog(this);
            }
        }

        #endregion

        /// <summary>
        /// The WindowManager class
        /// </summary>
        public WindowManager _windowManager;

        // This is handy when all the tabs are closed.
        private void tabControlEx_VisibleChanged(object sender, EventArgs e)
        {
            if (tabControlEx.Visible)
            {
                this.panel1.BackColor = SystemColors.Control;
            }
            else
                this.panel1.BackColor = SystemColors.AppWorkspace;
        }

        #region Printing & Print Preview
        private void Print()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintDialog();
        }

        private void PrintPreview()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintPreviewDialog();
        }
        #endregion

        #region Toolstrip buttons
        private void openWindowToolStripButton_Click(object sender, EventArgs e)
        {
            ExtendedWebBrowser newBrowser = this._windowManager.New();

            newBrowser.ObjectForScripting = new GetTourDocumentString();
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            //closes browser window
            //this._windowManager.Close();

            //closes admin tabPages
            tcAdmin.Visible = false;
        }

        private void tabEx_OnClose(object sender, CloseEventArgs e)
        {
            //this.userControl11.Controls.Remove(this.userControl11.TabPages[e.TabIndex]);

            //closes browser window
            this._windowManager.Close();
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            Print();
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        private void backToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoBack)
                _windowManager.ActiveBrowser.GoBack();
        }

        private void forwardToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoForward)
                _windowManager.ActiveBrowser.GoForward();
        }

        private void stopToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Stop();
            }
            stopToolStripButton.Enabled = false;
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Refresh(WebBrowserRefreshOption.Normal);
            }
        }

        private void homeToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoHome();
        }

        private void searchToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoSearch();
        }

        #endregion

        public WindowManager WindowManager
        {
            get { return _windowManager; }
        }

        /// <summary>
        /// load blocked terms from file
        /// </summary>
        /// <param name="path"></param>
        public void loadBlockedTerms(string path)
        {
            blockedTerms = File.ReadAllLines(path);

            if (!validateBlockedTerms())
            {
                //decrypt terms
                blockedTerms = Encryption.Decrypt(blockedTerms);
            }

            if (!validateBlockedTerms())
            {
                //log that terms have been tampered with
                log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedTerms.Dock = DockStyle.Fill;
            dgBlockedTerms.Anchor = AnchorStyles.Right;
            dgBlockedTerms.Anchor = AnchorStyles.Bottom;
            dgBlockedTerms.Anchor = AnchorStyles.Left;
            dgBlockedTerms.Anchor = AnchorStyles.Top;
            dgBlockedTerms.Columns.Add("Terms", "Terms");
            dgBlockedTerms.Refresh();

            foreach (string term in blockedTerms)
            {
                dgBlockedTerms.Rows.Add(new string[] { term });
            }
        }

        private void loadBlockedSites(string path)
        {
            blockedSites = File.ReadAllLines(path);

            if (!validateBlockedSites())
            {
                //decrypt terms
                blockedSites = Encryption.Decrypt(blockedSites);
            }

            if (!validateBlockedSites())
            {
                //log that terms have been tampered with
                log(blockedSitesPath, "Blocked Sites file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedSites.Dock = DockStyle.Fill;
            dgBlockedSites.Anchor = AnchorStyles.Right;
            dgBlockedSites.Anchor = AnchorStyles.Bottom;
            dgBlockedSites.Anchor = AnchorStyles.Left;
            dgBlockedSites.Anchor = AnchorStyles.Top;
            dgBlockedSites.Columns.Add("Sites", "Sites");

            foreach (string site in blockedSites)
            {
                dgBlockedSites.Rows.Add(new string[] { site });
            }
        }

        public void loadFoundBlockedTerms(string path)
        {
            string fBlockedTerms = "";

            if (File.Exists(path))
                foundBlockedTerms = File.ReadAllLines(path);

            if (foundBlockedTerms != null && foundBlockedTerms.Length > 0)
            {
                //if (!validateFoundBlockedTerms())
                //{
                //decrypt terms
                foundBlockedTerms = Encryption.Decrypt(foundBlockedTerms);
                //}

                if (!validateBlockedTerms())
                {
                    //log that terms have been tampered with
                    log(foundBlockedTermsPath, "Found Blocked Terms file has been tampered with.");
                    //block all pages
                    adminLock = true;
                }

                lbFoundBlockedTerms.DataSource = foundBlockedTerms;
            }
        }

        public void loadFoundBlockedSites(string path)
        {
            if (File.Exists(path))
                foundBlockedSites = File.ReadAllLines(path);

            if (foundBlockedSites != null && foundBlockedSites.Length > 0)
            {

                //if (!validateBlockedTerms())
                //{
                //decrypt terms
                foundBlockedSites = Encryption.Decrypt(foundBlockedSites);
                //}

                //if (!validateBlockedTerms())
                //{
                //    //log that terms have been tampered with
                //    log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //    //block all pages
                //    adminLock = true;
                //}

                lbFoundBlockedSites.DataSource = foundBlockedSites;
            }
        }

        private bool validateBlockedTerms()
        {
            bool isValid = false;

            foreach (string term in blockedTerms)
            {
                if (term.ToLower() == "fuck")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateBlockedSites()
        {
            bool isValid = false;

            foreach (string site in blockedSites)
            {
                if (site.ToLower() == "pussy.org")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateFoundBlockedTerms()
        {
            bool isValid = true;

            //foreach (string term in foundBlockedTerms)
            //{
            //    if (term.ToLower().Contains("fuck"))
            //    {
            //        isValid = true;
            //        break;
            //    }
            //}

            return isValid;
        }

        #region datagridview events
        private void dgBlockedTerms_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            //make sure values are valid
            //DataGridView dg = (DataGridView)sender;

        }

        private void dgBlockedTerms_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //update blocked terms file
                ArrayList terms = new ArrayList();
                string value = "";
                DataGridView dg = (DataGridView)sender;
                foreach (DataGridViewRow row in dg.Rows)
                {
                    value = Convert.ToString(row.Cells["Terms"].Value);
                    if (value != null && value.Trim().Length > 0)
                        terms.Add(value);
                }

                blockedTerms = (string[])terms.ToArray(typeof(string));

                //encrypt
                blockedTerms = Encryption.Encrypt(blockedTerms);

                //save blockedTerms
                File.WriteAllLines(blockedTermsPath, blockedTerms);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        private void logHeader(string path)
        {
            if (startTime.CompareTo(File.GetLastWriteTime(path)) == 1)
            {
                StringBuilder content = new StringBuilder();

                content.AppendLine();
                content.AppendLine("User: " + userName + "  Start Time: " + startTime);

                File.AppendAllText(path, Encryption.Encrypt(content.ToString()));
            }
        }

        public void log(string path, string content)
        {
            logHeader(path);

            File.AppendAllText(path, content);
        }

        public void log(string path, string[] content)
        {
            logHeader(path);

            File.WriteAllLines(path, content);
            //File.WriteAllText(path, content);
        }

        private void tcAdmin_VisibleChanged(object sender, EventArgs e)
        {
            closeToolStripButton.Visible = true;
        }

        private void loginTimer_Tick(object sender, EventArgs e)
        {
            loggedInTime++;

            if (loggedInTime > loginMaxTime)
            {
                loginTimer.Enabled = false;
                LoggedIn = false;
            }
        }

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            string[] loginInfo = { "username:" + txtNewUserName.Text.Trim(), "password:" + txtNewPassword.Text.Trim() };
            loginInfo = Encryption.Encrypt(loginInfo);
            File.WriteAllLines(MainForm.loginInfoPath, loginInfo);
            lblLoginInfoUpdated.Visible = true;
        }

        private void tpChangeLoginInfo_Leave(object sender, EventArgs e)
        {
            lblLoginInfoUpdated.Visible = false;
        }

        private string getDefaultBrowser()
        {
            //original value on classesroot
            //"C:\Program Files\Internet Explorer\IEXPLORE.EXE" -nohome

            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command",true);

                //trim off quotes
                //browser = key.GetValue(null).ToString().Replace("\"", "");
                //if (!browser.EndsWith(".exe"))
                //{
                //    //get rid of everything after the ".exe"
                //    browser = browser.Substring(0, browser.ToLower().LastIndexOf(".exe") + 4);
                //}

                browser = key.GetValue(null).ToString();
                
                //key.SetValue(null, (string)@browser);

                string safeSurfBrowser = "\""+ Application.ExecutablePath +"\"";

                key.SetValue(null, (string)@safeSurfBrowser);
            }
            finally
            {
                if (key != null) key.Close();
            }
            return browser;
        }

        private void JustinRecordtoolStripButton_Click(object sender, EventArgs e)
        {
            //need to get channel name from url
            string[] urlSegments = _windowManager.ActiveBrowser.Url.Segments;

            if (urlSegments[1].ToLower() != "directory")//this is a channel
            {
                string channelName = urlSegments[1];
                DialogResult result = MessageBox.Show("Are you sure you want to download from " + channelName, "Download " + channelName, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    //pop up justin downloader and start downloading
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader(channelName, "12", Application.StartupPath + "\\JustinDownloads\\");
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader();
                    //downloader.Channel = channelName;
                    //downloader.Show();
                }
            }
            else
                MessageBox.Show("You must be watching the channel you want to record");
        }

        private void toolStripButtonFavorites_Click(object sender, EventArgs e)
        {
            string url = "";

            //check for url
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.Url.PathAndQuery.Length > 0)
            {
                url = _windowManager.ActiveBrowser.Url.PathAndQuery;

                //add to onlineMedia.xml
                //SCTVObjects.MediaHandler.AddOnlineMedia(_windowManager.ActiveBrowser.Url.Host, _windowManager.ActiveBrowser.Url.PathAndQuery, "Online", "Favorites", "", "");
            }
            else
                MessageBox.Show("You must browse to a website to add it to your favorites");
        }

        private void showAddressBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.ShowAddressBar = showAddressBarToolStripMenuItem.Checked;

            showAddressBarToolStripMenuItem.Checked = !showAddressBarToolStripMenuItem.Checked;
        }

        private string findValue(string stringToParse, string startPattern, string endPattern)
        {
            return findValue(stringToParse, startPattern, endPattern, false);
        }

        private string findValue(string stringToParse, string startPattern, string endPattern, bool returnSearchPatterns)
        {
            int start = 0;
            int end = 0;
            string foundValue = "";

            try
            {
                start = stringToParse.IndexOf(startPattern);

                if (start > -1)
                {
                    if (!returnSearchPatterns)
                        stringToParse = stringToParse.Substring(start + startPattern.Length);
                    else
                        stringToParse = stringToParse.Substring(start);

                    end = stringToParse.IndexOf(endPattern);

                    if (end > 0)
                    {
                        if (returnSearchPatterns)
                            foundValue = stringToParse.Substring(0, end + endPattern.Length);
                        else
                            foundValue = stringToParse.Substring(0, end);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                //Tools.WriteToFile(ex);
            }

            return foundValue;
        }

        /// <summary>
        /// submit numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            //bitVideoBrowser.Url = new Uri("https://bitvideo.club/earn/offertoro");

            findSubmit(bitVideoBrowser.Document);
        }

        private void btnInstaGC_Click(object sender, EventArgs e)
        {
            //bitVideoBrowser.Url = new Uri("https://www.instagc.com/earn/offertoro/clicks");
        }

        /// <summary>
        /// quick pick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFaucetHub_Click(object sender, EventArgs e)
        {
            //findQuickPick(bitVideoBrowser.Document);
            findRevealAll(bitVideoBrowser.Document);
        }

        private void chbAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (!chbAutoRefresh.Checked)
            {
                //secondsTimer.Tag = null;
                //secondsTimer.Stop();

                lblRefreshTimer.Text = "0 seconds";
            }

            refreshUtilities.IsDisabled = !chbAutoRefresh.Checked;
        }

        [ComVisible(true)]
        public class GetTourDocumentString
        {
            public void CallServerSideCode()
            {
                try
                {
                    MainForm currentForm = ((MainForm)Application.OpenForms[0]);

                    var doc = currentForm.tourBrowser.Document;

                    var renderedHtml = doc.GetElementsByTagName("HTML")[0].OuterHtml;

                    currentForm.SetDocumentString = renderedHtml;
                }
                catch (Exception ex)
                {
                    //Application.Restart();
                }
            }
        }

        [ComVisible(true)]
        public class GetTourListDocumentString
        {
            public void CallServerSideCode()
            {
                try
                {
                    MainForm currentForm = ((MainForm)Application.OpenForms[0]);

                    var doc = currentForm.bitVideoBrowser.Document;

                    var renderedHtml = doc.GetElementsByTagName("HTML")[0].OuterHtml;

                    currentForm.SetTourListDocumentString = renderedHtml;
                }
                catch (Exception ex)
                {
                    //Application.Restart();
                }
            }
        }

        private void btnLoadSource_Click(object sender, EventArgs e)
        {
            
        }

        private void btnCheckForButton_Click(object sender, EventArgs e)
        {
            tourBrowser.Navigate("javascript: window.external.CallServerSideCode();");
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void btnSwitchUsers_Click(object sender, EventArgs e)
        {
            switchUsers();
        }

        private void logout()
        {
            refreshUtilities.GoToURL("https://www.rewardit.com/Home/Logout", 2, 0, true, lblRefreshTimer, bitVideoBrowser);

            if (!switchingUsers)
                currentUser = "";
        }

        private void login()
        {
            if (users.Count > 0 && switchingUsers)
            {
                if (currentUser.Length == 0)
                    getCurrentUser();

                if (currentUser.Length > 0)
                {
                    users.Remove(currentUser);

                    if (users.Count > 0)
                        currentUser = users[0];
                }
            }

            if (users.Count > 0)
            {
                //login
                //<a href="/users/sign_in">LOG IN</a>

                bool foundLogin = false;
                HtmlElementCollection elc = bitVideoBrowser.Document.GetElementsByTagName("a");

                foreach (HtmlElement el in elc)
                {
                    if (el.OuterHtml != null && el.OuterHtml.Contains("href=\"/users/sign_in\""))
                    {
                        refreshUtilities.ClickElement(el, 2, 0, lblRefreshTimer);

                        foundLogin = true;

                        break;
                    }
                }

                if(foundLogin)
                    populateUsernamePassword();
            }
            else
            {
                allDone = true;

                MessageBox.Show("All Done!");
            }

            switchingUsers = false;
        }

        private void populateUsernamePassword()
        {
            bool foundEmail = false;
            bool foundPassword = false;

            if (currentUser.Length == 0)
            {
                if (users.Count > 0)
                    currentUser = users[0];
            }

            //<input autofocus="autofocus" placeholder="Email" type="email" value="" name="user[email]" id="user_email">
            //<input autocomplete="off" placeholder="Password" type="password" name="user[password]" id="user_password">

            HtmlElementCollection elc = bitVideoBrowser.Document.GetElementsByTagName("input");

            foreach (HtmlElement el in elc)
            {
                if (!foundEmail && el.OuterHtml != null && el.OuterHtml.Contains("placeholder=\"Email\"") && el.OuterHtml.Contains("id=\"user_email\""))
                {
                    el.SetAttribute("value", currentUser.Split('|')[0]);

                    foundEmail = true;
                }

                if (el.OuterHtml != null && el.OuterHtml.Contains("placeholder=\"Password\"") && el.OuterHtml.Contains("id=\"user_password\""))
                {
                    el.SetAttribute("value", currentUser.Split('|')[1]);

                    foundPassword = true;

                    break;
                }
            }

            if (foundEmail && foundPassword)
            {
                //click login
                //<button class="WLButton ucase btn btn-success form-control loginbtn" tabindex="3" id="loginbtn_desktop">LOGIN</button>

                elc = bitVideoBrowser.Document.GetElementsByTagName("button");

                foreach (HtmlElement el in elc)
                {
                    if (el.OuterHtml != null && el.OuterHtml.Contains("id=\"loginbtn_desktop\""))
                    {
                        refreshUtilities.ClickElement(el, 2, 0, true, lblRefreshTimer);
                        loggingIn = false;
                        numberOfPrizesEntered = 0;
                        txtPrizeCount.Text = numberOfPrizesEntered.ToString();

                        break;
                    }
                }
            }
        }

        private void switchUsers()
        {
            if (users.Count > 1)
            {
                switchingUsers = true;

                logout();
            }
            else //we are done
            {
                switchingUsers = false;
                allDone = true;

                MessageBox.Show("All Done!");
            }
        }

        private string getCurrentUser()
        {
            string docString = bitVideoBrowser.DocumentText;

            foreach (string user in users)
            {
                if (docString.Contains(user.Split('|')[0]))
                {
                    currentUser = user;

                    return user;
                }
            }

            return "";
        }
    }
}