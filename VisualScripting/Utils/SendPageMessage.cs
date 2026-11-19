using Unity.VisualScripting;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// Posts text to the space page as a "browser-message" event on the page's object for <see cref="target"/>
    /// (scene.objects[id].addEventListener("browser-message", e =&gt; e.detail)). Unlike Inject BullSchript it evaluates
    /// no script and waits for no reply: the text rides the bridge's normal batched send to the page, once. For graphs
    /// that report to their page every frame. The page needs the object (one it created, or one it was told about).
    /// </summary>
    [UnitTitle("Send Page Message")]
    [UnitShortTitle("Send Page Message")]
    [UnitCategory("BS\\Browser")]
    [TypeIcon(typeof(BSObjectId))]
    public class SendPageMessage : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        [NullMeansSelf]
        public ValueInput target;

        [DoNotSerialize]
        public ValueInput message;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                var go = flow.GetValue<GameObject>(target);
                var text = flow.GetValue<string>(message);
                var link = BSScene.Instance().link;
                if (go != null && link != null)
                {
                    link.Send(APICommands.EVENT + APICommands.BROWSER_MESSAGE + MessageDelimiters.PRIMARY + BSScene.UnityId(go)
                              + MessageDelimiters.SECONDARY + WireText.Encode(text ?? ""));
                }
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            target = ValueInput<GameObject>("Target", null).NullMeansSelf();
            message = ValueInput("Message", string.Empty);
            Requirement(target, inputTrigger);
            Requirement(message, inputTrigger);
            Succession(inputTrigger, outputTrigger);
        }
    }
}
