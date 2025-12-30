// using InteLuk.API.BusinessObjects;

// namespace Sarah.Rules.Conditions
// {
//     public class ImpftermineAvailableCondition : RuleCondition
//     {
//         public ImpftermineAvailableCondition() : base(0)
//         {
//         }

//         public override bool Evaluate(NetworkEvent evt)
//         {
//             ImpftermineChangedEvent impftermineChangedEvent = evt as ImpftermineChangedEvent;
//             if (impftermineChangedEvent != null)
//             {
//                 return (impftermineChangedEvent.IsTerminAvailable);
//             }
//             return false;
//         }
//     }
// }
