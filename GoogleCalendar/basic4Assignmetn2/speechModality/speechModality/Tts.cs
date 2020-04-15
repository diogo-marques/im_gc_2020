using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Media;
using Microsoft.Speech.Synthesis;
using Microsoft.Speech.AudioFormat;




namespace speechModality
{
    class Tts
    {
        SpeechSynthesizer tts = null;
        static SoundPlayer player = new SoundPlayer();

        /*
         * Text to Speech
         */
        public Tts()
        {


            Console.WriteLine("TTS constructor called");



            //create sound player
            //player = new SoundPlayer();

            //create speech synthesizer
            tts = new SpeechSynthesizer();


            // show voices 
            // Initialize a new instance of the SpeechSynthesizer.
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {

                // Output information about all of the installed voices. 
                Console.WriteLine("Installed voices -");
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    string AudioFormats = "";
                    foreach (SpeechAudioFormatInfo fmt in info.SupportedAudioFormats)
                    {
                        AudioFormats += String.Format("{0}\n",
                        fmt.EncodingFormat.ToString());
                    }

                    Console.WriteLine(" Name:          " + info.Name);
                    Console.WriteLine(" Culture:       " + info.Culture);
                    Console.WriteLine(" Age:           " + info.Age);
                    Console.WriteLine(" Gender:        " + info.Gender);
                    Console.WriteLine(" Description:   " + info.Description);
                    Console.WriteLine(" ID:            " + info.Id);
                    Console.WriteLine(" Enabled:       " + voice.Enabled);
                    if (info.SupportedAudioFormats.Count != 0)
                    {
                        Console.WriteLine(" Audio formats: " + AudioFormats);
                    }
                    else
                    {
                        Console.WriteLine(" No supported audio formats found");
                    }

                    string AdditionalInfo = "";
                    foreach (string key in info.AdditionalInfo.Keys)
                    {
                        AdditionalInfo += String.Format("  {0}: {1}\n", key, info.AdditionalInfo[key]);
                    }

                    Console.WriteLine(" Additional Info - " + AdditionalInfo);
                    Console.WriteLine();
                }
            }
            //Console.WriteLine("Press any key to exit...");
            //Console.ReadKey();




            //set voice
            tts.SelectVoiceByHints(VoiceGender.Male, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("pt-PT"));

            //tts.SelectVoice("...")


            //set function to play audio after synthesis is complete
            tts.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(tts_SpeakCompleted);


        }

        /*
         * Speak
         * 
         * @param text - text to convert
         */
        public void Speak(string text)
        {
            while (player.Stream != null)
            {
               Console.WriteLine("Waiting...");
            }

            //create audio stream with speech
            player.Stream = new System.IO.MemoryStream();
            tts.SetOutputToWaveStream(player.Stream);
            tts.SpeakAsync(text);
        }

        public void Speak(string text, int rate)
        {

            Console.WriteLine("Speak method called , version with samplerate parameter");

            while (player.Stream != null)
            {
                Console.WriteLine("Waiting...");
            }


            //create audio stream with speech
            player.Stream = new System.IO.MemoryStream();
            tts.SetOutputToWaveStream(player.Stream);
            tts.Rate = rate;
            // tts.SpeakAsync(text);


            Console.WriteLine("... calling  SpeakSsmlAsync()");

            tts.SpeakSsmlAsync(text);

            Console.WriteLine("done  SpeakSsmlAsync().\n");

        }

        /*
         * tts_SpeakCompleted
         */
        void tts_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (player.Stream != null)
            {
                //play stream
                player.Stream.Position = 0;
                player.Play();
                player.Stream = null;  //  NEW 2015
            }
        }
    }
}
