namespace MonkeyJumpersButtons
{
    using UnityEngine;
    using Photon.Pun;
    using Photon.Realtime;

    public class ReportButton : MonoBehaviour
    {
        [SerializeField] public int ButtonNumber;
        [SerializeField] public LeaderBoard LB;
        public string HandTag = "finger";
        public Material PressedMaterial;
        private Material UnPressedMaterial;
        private Renderer rend;

        private Player ReportedPlayer;

        private bool reported;

        public string reportReason;

        private void Start()
        {
            rend = GetComponent<Renderer>();
            UnPressedMaterial = rend.material;
        }

        private void Update()
        {
            if (ButtonNumber > 0 && ButtonNumber <= PhotonNetwork.PlayerList.Length)
            {
                if (PhotonNetwork.PlayerList[ButtonNumber - 1] != ReportedPlayer && reported)
                {
                    reported = false;
                    rend.material = UnPressedMaterial;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ButtonNumber > 0 && ButtonNumber <= PhotonNetwork.PlayerList.Length)
            {
                if (other.CompareTag(HandTag) && !reported)
                {
                    LB.Report(ButtonNumber, reportReason); 
                    rend.material = PressedMaterial;
                    ReportedPlayer = PhotonNetwork.PlayerList[ButtonNumber - 1];
                    reported = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(HandTag))
            {
                rend.material = UnPressedMaterial;
            }
        }
    }
}
