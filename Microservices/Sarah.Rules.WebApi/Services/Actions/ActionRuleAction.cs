using Sarah.API.BusinessObjects;
using System;
using System.Threading.Tasks;

namespace Sarah.Rules.Actions
{
    /// <summary>
    /// Beispielimplementierung einer Aktion, die beim Zutreffen einer Regel ausgeführt wird
    /// (via Action-Callback)
    /// </summary>
    public class ActionRuleAction : RuleAction
    {
        private Action<NetworkEvent> ExecuteAction { get; }
        private readonly ILogger _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="executeAction">Callback der bei Execute aufgerufen wird</param>
        public ActionRuleAction(Action<NetworkEvent> executeAction, ILogger logger)
        {
            if(executeAction == null)
            {
                throw new ArgumentNullException(nameof(executeAction));
            }
            this.ExecuteAction = executeAction;
            this._logger = logger;
        }


        /// <summary>
        /// Wird aufgerufen, wenn die zugeordnete Regel zutrifft
        /// </summary>
        public override void Execute(NetworkEvent sourceEvent)
        {
            try
            {
                this.ExecuteAction.Invoke(sourceEvent);
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Ausführen einer ActionRuleAction");
            }
        }
    }

    /// <summary>
    /// Beispielimplementierung einer Aktion, die beim Zutreffen einer Regel ausgeführt wird
    /// (via Task)
    /// </summary>
    public class TaskRuleAction : RuleAction
    {
        private Func<Task> TaskFactory { get; }
        private readonly ILogger _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="executeAction">Callback der bei Execute aufgerufen wird</param>
        public TaskRuleAction(Func<Task> taskFactory, ILogger logger)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException(nameof(taskFactory));
            }
            this.TaskFactory = taskFactory;
            this._logger = logger;
        }


        /// <summary>
        /// Wird aufgerufen, wenn die zugeordnete Regel zutrifft
        /// </summary>
        public override void Execute(NetworkEvent sourceEvent)
        {
            try
            {
                Task promise = Task.Run(this.TaskFactory);
                promise.Wait(TimeSpan.FromMinutes(2)); // warte max 2 Minuten auf Ende der Ausführung, das ist lang genug
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Ausführen einer TaskRuleAction");
            }
        }
    }

}
