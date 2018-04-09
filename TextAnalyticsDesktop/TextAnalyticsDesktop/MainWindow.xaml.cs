using System;
using System.Collections.Generic;
using System.Linq;
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

using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TextAnalyticsDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    class Question
    {
        public int votes { get; set; }
        public string text { get; set; }
        public bool answered { get; set; }
        public Question(int v, string t)
        {
            votes = v;
            text = t;
            answered = false;
        }

        public void incrementVotes() { ++votes; }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TextToAnalyze.Text ="The Taylor series ansatz is the most straight forward but numerically very instable method \n" + 
                            "of parameterizing a continuous wave function by a ﬁnite set of discrete numbers.For very \n" +
                            "short expansions (N. 10) , the Taylor series improves compared to the \n" + 
                            "FD - scheme.We have not discussed boundary conditions for the ansatz. Mathematically \n "  + 
                             "the the overlap matrix S is invertible, however, numerically this inversion is extremely unstable.";

        }

        

        List<Question> storedKeywords = new List<Question>();
        int questionIndex = 0;
        

        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {
            Analyze.IsEnabled = false;

            using (var client = new HttpClient())
            {
                Debug.Text = "";
                Questions.Text = "";
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "c24804c36d174fc1a4535b2be2085ff8");

                string contentString = @"{""documents"": [{""language"": ""en"",""id"": ""1"",""text"": """ + TextToAnalyze.Text + @""" }]}";
                var content = new StringContent(contentString);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                string stringResponse = String.Empty;
                try
                {

                    Uri KeyPhrase = new Uri("https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases");
                    var response = await client.PostAsync(KeyPhrase, content);
                    stringResponse = await response.Content.ReadAsStringAsync();
                }
                catch(Exception ex)
                {

                    MessageBox.Show(ex.Message, "Are you connected to the internet?");
                    return;
                }
                

                Debug.Text = stringResponse;

                string[] strKeyPhrases;
                try
                {

                    dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(stringResponse);

                    var keyPhrases = stuff.documents[0].keyPhrases;

                    strKeyPhrases = new string[keyPhrases.Count];

                    for(int i = 0; i < strKeyPhrases.Length; i++)
                    {

                        strKeyPhrases[i] = keyPhrases[i];

                    }

                    

                }catch(Exception ex)
                {

                    MessageBox.Show(ex.Message);
                    return;
                }

                foreach(var str in strKeyPhrases)
                {

                    Questions.Text += "Define " + str + '\n';
                    storedKeywords.Add(new Question(0,str));

                }

                writeLearningScore();



                /*
                JsonObject jsonObject = JsonValue.Parse(stringResponse).GetObject();
                var jsonDocument = jsonObject.GetNamedArray("documents");
                var jsonKeyPhrases = jsonDocument.GetObjectAt(0).GetNamedArray("keyPhrases");

                string[] keyPhraseStrings = new string[jsonKeyPhrases.Count];
                for (uint i = 0; i < jsonKeyPhrases.Count; i++)
                {

                    keyPhraseStrings[i] = jsonKeyPhrases.GetStringAt(i);

                }

                for (int i = 0; i < keyPhraseStrings.Length; i++)
                {

                    Questions.Text += "Define " + keyPhraseStrings[i] + '\n';

                }

                */

            }
        }

        private void AskQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (storedKeywords.Count() == 0) return;
            Random rnd = new Random();
            int newIndex = rnd.Next(0, storedKeywords.Count());
            while(questionIndex == newIndex)
            {
                newIndex = rnd.Next(0, storedKeywords.Count());
            }
            questionIndex = newIndex;
            randomQuestion.Text = "Define "+storedKeywords[questionIndex].text;
            if(storedKeywords[questionIndex].answered == false) solvedQuestion.IsChecked = false;
            if (storedKeywords[questionIndex].answered == true) solvedQuestion.IsChecked = true;
        }

        private void UpvoteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (storedKeywords.Count() == 0) return;
            storedKeywords[questionIndex].incrementVotes();
            Debug.Text = "Upvotes for " + storedKeywords[questionIndex].text + ": " + storedKeywords[questionIndex].votes;
        }

        private void ShowQuestionRank_Click(object sender, RoutedEventArgs e)
        {
            if (storedKeywords.Count() == 0) return;
            var orderedQuestions =
                from q in storedKeywords
                orderby q.votes descending
                select q;
            Debug.Clear();
            foreach(Question q in orderedQuestions)
            {
                Debug.Text += q.text + ": " + q.votes + " votes (Answered: "+q.answered+")\n";
            }
        }

        private void solvedQuestion_Checked(object sender, RoutedEventArgs e)
        {
            if (storedKeywords.Count() == 0) return;
            storedKeywords[questionIndex].answered = true;
            writeLearningScore();
        }

        private void writeLearningScore()
        {
            int answeredQuestions = 0;
            int totalQuestions = storedKeywords.Count();
            foreach(Question q in storedKeywords)
            {
                if (q.answered) answeredQuestions++;
            }
            if (answeredQuestions == totalQuestions) LearningScore.Text = "You've answered all of the " + totalQuestions + "available questions!";
            else LearningScore.Text = answeredQuestions + " of " + totalQuestions + " solved!";
        }

        private void solvedQuestion_Unchecked(object sender, RoutedEventArgs e)
        {
            storedKeywords[questionIndex].answered = false;
            writeLearningScore();
        }
    }
}
