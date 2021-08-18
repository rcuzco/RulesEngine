// Copyright (c) Microsoft Corporation.
//  Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using static RulesEngine.Extensions.ListofRuleResultTreeExtension;

namespace DemoApp
{
    public class GoalSystemsDemo
    {
        public void Run()
        {
            #region REGLA 1
            /*
                 la suma de todos los descansos entre eventos de conducción en una jornada debe 
                ser como mínimo 70 minutos (4200 segundos), siempre que el descanso sea más 
                largo de 15 minutos (15 mins son 900 segundos)
                 */

            #region Datos que vendrían de fuera
            Jornada jornada = new Jornada();
            jornada.EventosDeConduccion = new List<EventoDeConduccion>()
            {
                new EventoDeConduccion()
                {
                    Fecha = DateTime.Now,
                    HoraInicio = 21600,  /*6.00*/
                    HoraFin = 32400, /*9.00*/
                },
                new EventoDeConduccion()
                {
                    Fecha = DateTime.Now,
                    HoraInicio = 34200,  /*9.30*/
                    HoraFin = 45000, /*12.30*/
                },
                new EventoDeConduccion()
                {
                    Fecha = DateTime.Now,
                    HoraInicio = 46200,  /*12.50*/
                    HoraFin = 57000, /*15.50*/
                },
                new EventoDeConduccion()
                {
                    Fecha = DateTime.Now,
                    HoraInicio = 57600,  /*16.00*/
                    HoraFin = 68400, /*19.00*/
                }
            };
            jornada.Descansos = new List<Descanso>
            {
                new Descanso()  { HoraInicio = 32460, HoraFin = 34140 },
                new Descanso()  { HoraInicio = 45060, HoraFin = 46140 },
                new Descanso()  { HoraInicio = 57060, HoraFin = 57540 }
            };
            //la cantidad de descansos siempre será igual a la cantidad de eventos de conducción menos 1
            #endregion

            var sumaDescansos = jornada.Descansos.Where(d => d.Duracion > 900).Sum(d => d.Duracion);
            dynamic ruleInputData = new ExpandoObject();
            ruleInputData.sumaDescansos = sumaDescansos;

            Console.WriteLine($"Running {nameof(GoalSystemsDemo)}....");
            List<WorkflowRules> workFlowRules = new List<WorkflowRules>();
            WorkflowRules workflowRule = new WorkflowRules();
            workflowRule.WorkflowName = "Test Workflow Rule 1";

            List<Rule> rules = new List<Rule>();

            Rule rule = new Rule();
            rule.RuleName           = "SumaDescansosMinimo70Minutos";
            rule.SuccessEvent       = "Regla pasada";
            rule.ErrorMessage       = "Regla no pasada";
            rule.Expression         = "sumaDescansos >= 4200";
            rule.RuleExpressionType = RuleExpressionType.LambdaExpression;

            rules.Add(rule);
            #endregion

            #region REGLA 2
            //REGLA TELEPORT
            List<DayTaskForReadingData> tareasAVerificar = new List<DayTaskForReadingData>();

            Rule ruleTeleport = new Rule();
            ruleTeleport.RuleName           = "Teleport";
            ruleTeleport.SuccessEvent       = "Regla pasada";
            ruleTeleport.ErrorMessage       = "Regla no pasada";
            ruleTeleport.Expression         = "previousTask.ResourceID == currentTask.ResourceID AND previousTask.NextColDay == currentTask.ColDay AND previousTask.StnEndId != currentTask.StnStartId";
            ruleTeleport.RuleExpressionType = RuleExpressionType.LambdaExpression;
            
            rules.Add(ruleTeleport);

            var converter = new ExpandoObjectConverter();
            dynamic previousTask = JsonConvert.DeserializeObject<ExpandoObject>(null, converter);
            dynamic currentTask = JsonConvert.DeserializeObject<ExpandoObject>(null, converter);

            #endregion







            workflowRule.Rules = rules;
            workFlowRules.Add(workflowRule);
            var businessRuleEngine = new RulesEngine.RulesEngine(workFlowRules.ToArray(), null);


            var inputs = new dynamic[]
              {
                    ruleInputData,
                    previousTask,
                    currentTask
              };

            List<RuleResultTree> resultList = businessRuleEngine.ExecuteAllRulesAsync("Test Workflow Rule 1", inputs).Result;

            bool outcome = false;

            //Different ways to show test results:
            outcome = resultList.TrueForAll(r => r.IsSuccess);

            resultList.OnSuccess((eventName) => {
                Console.WriteLine($"Result '{eventName}' is as expected.");
                outcome = true;
            });

            resultList.OnFail(() => {
                outcome = false;
            });

            Console.WriteLine($"Test outcome: {outcome}.");
        }
    }

    internal class Descanso
    {
        public int HoraInicio { get; internal set; }
        public int HoraFin { get; internal set; }
        public int Duracion {
            get {
                return HoraFin - HoraInicio;
            }
        }
    }

    internal class EventoDeConduccion
    {
        public DateTime Fecha { get; internal set; }
        public int HoraInicio { get; internal set; }
        public int HoraFin { get; internal set; }
    }

    internal class Jornada
    {
        public List<EventoDeConduccion> EventosDeConduccion { get; internal set; }
        public List<Descanso> Descansos { get; internal set; }
    }

    public class DayTaskForReadingData
    {
        /// <summary>
        /// Property that indicates the id of the task
        /// </summary>
        public Guid? TaskId { get; set; }

        /// <summary>
        /// Property that indicates the id of the task tables's entity field
        /// </summary>
        public Guid? TaskEntityId { get; set; }

        /// <summary>
        /// Property that indicates the id of the absence
        /// </summary>
        public Guid? AbsenceId { get; set; }

        /// <summary>
        /// Property that indicates the id of the resourse
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Property that indicates the date since the task starts
        /// </summary>
        public DateTime ColDay { get; set; }

        /// <summary>
        /// Property that indicates the name of the task
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Property that indicates the type of the day of the task
        /// </summary>
        public int? DayTypeId { get; set; }

        /// <summary>
        /// Property that indicates the subtype of the day of the task
        /// </summary>
        public int? DaySubTypeId { get; set; }

        /// <summary>
        /// Property that indicates the number of days in which the task starts from the colDay property
        /// </summary>
        public int? DayStart { get; set; }

        /// <summary>
        /// Property that indicates the time the task starts
        /// </summary>
        public int? HourStart { get; set; }

        /// <summary>
        /// Property that indicates the number of days in which the task ends from the colDay property
        /// </summary>
        public int? DayEnd { get; set; }

        /// <summary>
        /// Property that indicates the time the task ends
        /// </summary>
        public int? HourEnd { get; set; }

        /// <summary>
        /// Property that indicates the name of the station from where the task starts
        /// </summary>
        public string StnStart { get; set; }

        /// <summary>
        /// Property that indicates the name of the station where the task ends
        /// </summary>
        public string StnEnd { get; set; }

        /// <summary>
        /// Property that indicates the list of shifts associated with the task
        /// </summary>
        public List<int> Shifts { get; set; }

 

        /// <summary>
        /// Property that indicates the identity from table SolutionRow
        /// </summary>
        public Guid SolutionRowId { get; set; }

        /// <summary>
        /// Property that indicates the order
        /// </summary>
        public int NOrder { get; set; }

        /// <summary>
        /// Property that indicates if the dayTask has a note
        /// </summary>
        public bool HasNote { get; set; }

        /// <summary>
        /// Property that indicates if it is filtered or not
        /// </summary>
        public bool Filtered { get; set; } = true;

        /// <summary>
        /// Property that indicates the id of the station from where the task starts
        /// </summary>
        public Guid? StnStartId { set; get; }

        /// <summary>
        /// Property that indicates the id of the station where the task ends
        /// </summary>
        public Guid? StnEndId { set; get; }
    }
}
