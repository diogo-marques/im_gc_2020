using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mmisharp;
using Microsoft.Speech.Recognition;


namespace speechModality
{
    public class SpeechMod
    {
        private Tts t = new Tts();
        private SpeechRecognitionEngine sre;
        private Grammar gr;
        public event EventHandler<SpeechEventArg> Recognized;
        protected virtual void onRecognized(SpeechEventArg msg)
        {
            EventHandler<SpeechEventArg> handler = Recognized;
            if (handler != null)
            {
                handler(this, msg);
            }
        }

        private LifeCycleEvents lce;
        private MmiCommunication mmic;

        public SpeechMod()
        {
            //init LifeCycleEvents..
            lce = new LifeCycleEvents("ASR", "FUSION","speech-1", "acoustic", "command"); // LifeCycleEvents(string source, string target, string id, string medium, string mode)
            //mmic = new MmiCommunication("localhost",9876,"User1", "ASR");  //PORT TO FUSION - uncomment this line to work with fusion later
            mmic = new MmiCommunication("localhost", 8000, "User1", "ASR"); // MmiCommunication(string IMhost, int portIM, string UserOD, string thisModalityName)

            mmic.Send(lce.NewContextRequest());

            //load pt recognizer
            sre = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("pt-PT"));
            gr = new Grammar(Environment.CurrentDirectory + "\\ptG.grxml", "rootRule");
            sre.LoadGrammar(gr);

            
            sre.SetInputToDefaultAudioDevice();
            sre.RecognizeAsync(RecognizeMode.Multiple);
            sre.SpeechRecognized += Sre_SpeechRecognized;
            sre.SpeechHypothesized += Sre_SpeechHypothesized;

        }

        private void Sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            onRecognized(new SpeechEventArg() { Text = e.Result.Text, Confidence = e.Result.Confidence, Final = false });
        }

        bool confirmationFlag = false;
        string prev_json;
        
        private void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            onRecognized(new SpeechEventArg(){Text = e.Result.Text, Confidence = e.Result.Confidence, Final = true});
            String exNot;
            //SEND
            // IMPORTANT TO KEEP THE FORMAT {"recognized":["SHAPE","COLOR"]}
            if (e.Result.Confidence < 0.65)
            {
                t.Speak("Repita a frase, por favor.");
            }
            else
            {
                string json = "{ \"recognized\": [";
                foreach (var resultSemantic in e.Result.Semantics)
                {
                    json += "\"" + resultSemantic.Value.Value + "\", ";
                }
                json = json.Substring(0, json.Length - 2);
                json += "] }";

                Console.WriteLine(json);
                if (confirmationFlag)
                {
                    if (e.Result.Semantics.First().Value.Value.Equals("YES"))
                    {
                        exNot = lce.ExtensionNotification(e.Result.Audio.StartTime + "", e.Result.Audio.StartTime.Add(e.Result.Audio.Duration) + "", e.Result.Confidence, prev_json);
                        mmic.Send(exNot);
                        confirmationFlag = false;
                    }
                    else if(e.Result.Semantics.First().Value.Value.Equals("NO"))
                    {
                        confirmationFlag = false;
                    }
                }
                else
                {
                    if (e.Result.Semantics.First().Value.Value.Equals("CREATE_EVENT"))
                    {
                        confirmationFlag = true;
                        prev_json = json;
                        if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TOMORROW"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TODAY"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " no dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende marcar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " no dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }

                    }else if (e.Result.Semantics.First().Value.Value.Equals("CANCEL_EVENT"))
                    {
                        confirmationFlag = true;
                        prev_json = json;
                        if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TOMORROW"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TODAY"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " do dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende cancelar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " do dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }

                    }
                    else if (e.Result.Semantics.First().Value.Value.Equals("CANCEL_EVENTS_DAY"))
                    {
                        confirmationFlag = true;
                        prev_json = json;
                        if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TOMORROW"))
                        {
                             t.Speak("Pretende cancelar os eventos de amanhã?" );
                            
                        }
                        else if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TODAY"))
                        {
                            t.Speak("Pretende cancelar os eventos de hoje?");
                        }
                        else
                        {
                            t.Speak("Pretende cancelar os eventos  do dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) );
                        }

                    }else if (e.Result.Semantics.First().Value.Value.Equals("POSTPONE_EVENT"))
                    {
                        confirmationFlag = true;
                        prev_json = json;
                        if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TOMORROW"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " amanhã às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else if (e.Result.Semantics.ElementAt(2).Value.Value.Equals("TODAY"))
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " hoje às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }
                        else
                        {
                            if (e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] == "0")
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " do dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às  " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas?");
                            }
                            else
                            {
                                t.Speak("Pretende adiar o evento " + e.Result.Semantics.ElementAt(1).Value.Value + " do dia " + sToDateS(e.Result.Semantics.ElementAt(2).Value.Value.ToString()) + " às " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[0] + " horas e " + e.Result.Semantics.ElementAt(3).Value.Value.ToString().Split(':')[1] + " minutos?");
                            }
                        }

                    }
                    else
                    {
                        exNot = lce.ExtensionNotification(e.Result.Audio.StartTime + "", e.Result.Audio.StartTime.Add(e.Result.Audio.Duration) + "", e.Result.Confidence, json);
                        mmic.Send(exNot);
                    }
                }

                
            }

            
        }
        private String sToDateS(String s)
        {
            string[] d = s.Split(' ');
            string r = d[0] + " de ";
            if(d[1] == "1")
            {
                r += "janeiro";
            }else if(d[1] == "2")
            {
                r += "fevereiro";
            }
            else if (d[1] == "3")
            {
                r += "março";
            }
            else if (d[1] == "4")
            {
                r += "abril";
            }
            else if (d[1] == "5")
            {
                r += "maio";
            }
            else if (d[1] == "6")
            {
                r += "junho";
            }
            else if (d[1] == "7")
            {
                r += "julho";
            }
            else if (d[1] == "8")
            {
                r += "agosto";
            }
            else if (d[1] == "9")
            {
                r += "setembro";
            }
            else if (d[1] == "10")
            {
                r += "outubro";
            }
            else if (d[1] == "11")
            {
                r += "novembro";
            }
            else if (d[1] == "12")
            {
                r += "dezembro";
            }
            return r;
        }
    }
}
