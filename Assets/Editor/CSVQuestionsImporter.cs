using UnityEngine;
using UnityEditor;
using System.IO;

public class CSVQuestionsImporter
{
    [MenuItem("Tools/Importar Preguntas desde CSV")]
    public static void ImportQuestions()
    {
        string path = EditorUtility.OpenFilePanel("Seleccionar archivo CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path); //Separa las filas (osea cada pregunta con sus correspondientes valores) y las guarda en un arreglo.

        for (int i = 1; i < lines.Length; i++) //Saltea la primera fila pues son los títulos de las columnas.
        {
            string[] values = lines[i].Split(','); //Separa las columnas y guarda los valores en un arreglo.

            if (values.Length < 6)
            {
                Debug.LogWarning($"Línea {i + 1} incompleta.");
                continue;
            }

            //Crea una nueva pregunta (scriptable object) con los valores correspondientes en cada variable.
            QuestionData newQuestion = ScriptableObject.CreateInstance<QuestionData>();
            newQuestion.question = values[0];
            newQuestion.correctAnswer = values[1];
            newQuestion.wrongAnswers = new string[] { values[2], values[3], values[4] };
            if (System.Enum.TryParse(values[5], true, out QuestionData.Difficulty diff))
            {
                newQuestion.difficulty = diff;
            }
            
            if (System.Enum.TryParse(values[6], true, out QuestionData.Subject sub))
            {
                newQuestion.subject = sub;
            }

            string fileName = $"Pregunta_{i}_{newQuestion.difficulty}_{newQuestion.subject}";
            string assetPath = $"Assets/Resources/en/{fileName}.asset";

            AssetDatabase.CreateAsset(newQuestion, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Preguntas importadas correctamente.");
    }
}
