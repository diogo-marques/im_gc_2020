using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Microsoft.Speech.Recognition;
using Microsoft.Speech.Recognition.SrgsGrammar;

class SpeechRecognizer
{

    private SpeechRecognitionEngine sr;

    /*
     * SpeechRecognizer
     * 
     * 
     * @param GName - grammar file name
     */
    public SpeechRecognizer(string GName)
    {
        //creates the speech recognizer engine
        sr = new SpeechRecognitionEngine();
        sr.SetInputToDefaultAudioDevice();


        Grammar gr = null;

        //verifies if file exist, and loads the Grammar file, else load defualt grammar
        if (System.IO.File.Exists(GName))
        {

            gr = new Grammar(GName);
            gr.Enabled = true;

            Console.WriteLine("Grammar Loaded");
        }
        else
        {
            GrammarBuilder gb = new GrammarBuilder(new Choices("texto", "aula"));
            gb.Culture = sr.RecognizerInfo.Culture;

            gr = new Grammar(gb);

            Console.WriteLine("Loaded default grammar");

        }

        //load Grammar to speech engine
        sr.LoadGrammar(gr);

        //assigns a method, to execute when speech is recognized
        sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(SpeechRecognized);

    }

    /*
     * SpeechRecognized
     * 
     * EventHandler
     * 
     * 
    */
    private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        //gets recognized text
        string text = e.Result.Text;

        Console.WriteLine(text);


    }

}
