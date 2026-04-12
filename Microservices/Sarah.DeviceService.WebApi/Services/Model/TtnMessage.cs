using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.Ttn
{

    // TtnMessage myDeserializedClass = JsonConvert.DeserializeObject<TtnMessage>(myJsonResponse);
    public class ApplicationIds
    {
        public string application_id { get; set; } = "";
    }

    public class EndDeviceIds
    {
        public string device_id { get; set; } = "";
        public ApplicationIds application_ids { get; set; } = new ApplicationIds();
        public string dev_eui { get; set; } = "";
        public string join_eui { get; set; } = "";
        public string dev_addr { get; set; } = "";
    }

    public class GatewayIds
    {
        public string gateway_id { get; set; } = "";
        public string eui { get; set; } = "";
    }

    public class RxMetadata
    {
        public GatewayIds gateway_ids { get; set; } = new GatewayIds();
        public long timestamp { get; set; }
        public int rssi { get; set; }
        public int channel_rssi { get; set; }
        public double snr { get; set; }
        public string uplink_token { get; set; } = "";
        public int channel_index { get; set; }
    }

    public class Lora
    {
        public int bandwidth { get; set; }
        public int spreading_factor { get; set; }
    }

    public class DataRate
    {
        public Lora lora { get; set; } = new Lora();
    }

    public class Settings
    {
        public DataRate data_rate { get; set; } = new DataRate();
        public int data_rate_index { get; set; }
        public string coding_rate { get; set; } = "";
        public string frequency { get; set; } = "";
        public long timestamp { get; set; }
    }

    public class VersionIds
    {
        public string brand_id { get; set; } = "";
        public string model_id { get; set; } = "";
        public string hardware_version { get; set; } = "";
        public string firmware_version { get; set; } = "";
        public string band_id { get; set; } = "";
    }

    public class NetworkIds
    {
        public string net_id { get; set; } = "";
        public string tenant_id { get; set; } = "";
        public string cluster_id { get; set; } = "";
    }

    public class UplinkMessage
    {
        public string session_key_id { get; set; } = "";
        public int f_port { get; set; }
        public int f_cnt { get; set; }
        public string frm_payload { get; set; } = "";
        public List<RxMetadata> rx_metadata { get; set; } = new List<RxMetadata>();
        public Settings settings { get; set; } = new Settings();
        public string received_at { get; set; } = "";
        public string consumed_airtime { get; set; } = "";
        public VersionIds version_ids { get; set; } = new VersionIds();
        public NetworkIds network_ids { get; set; } = new NetworkIds();
    }

    public class TtnMessage
    {
        public EndDeviceIds end_device_ids { get; set; } = new EndDeviceIds();
        public List<string> correlation_ids { get; set; } = new List<string>();
        public string received_at { get; set; } = "";
        public UplinkMessage uplink_message { get; set; } = new UplinkMessage();
    }


}
