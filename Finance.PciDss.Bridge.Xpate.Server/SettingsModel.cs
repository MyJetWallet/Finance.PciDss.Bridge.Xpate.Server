using SimpleTrading.SettingsReader;

namespace Finance.PciDss.Bridge.Xpate.Server
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("PciDssBridgeXpate.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpatePciDssBaseUrl")]
        public string XpatePciDssBaseUrl { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateKycVerifiedAmountinUsd")]
        public string XpateKycVerifiedAmountinUsd { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateKycVerifiedEndpointId")]
        public string XpateKycVerifiedEndpointId { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateEndpointId")]
        public string XpateEndpointId { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateLogin")]
        public string XpateLogin { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateMerchantControl")]
        public string XpateMerchantControl { get; set; }

        [YamlProperty("PciDssBridgeXpate.DefaultRedirectUrl")]
        public string DefaultRedirectUrl { get; set; }

        [YamlProperty("PciDssBridgeXpate.CallbackUrl")]
        public string CallbackUrl { get; set; }

        //{brand}@{prefix}@{redirectUrl}|{brand}@{prefix}@{redirectUrl} 
        [YamlProperty("PciDssBridgeXpate.RedirectUrlMapping")]
        public string RedirectUrlMapping { get; set; }

        [YamlProperty("PciDssBridgeXpate.AuditLogGrpcServiceUrl")]
        public string AuditLogGrpcServiceUrl { get; set; }

        [YamlProperty("PciDssBridgeXpate.ConvertServiceGrpcUrl")]
        public string ConvertServiceGrpcUrl { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateStatusRequestRetriesCount")]
        public int XpateStatusRequestRetriesCount { get; set; }

        [YamlProperty("PciDssBridgeXpate.XpateStatusRequestRetriesDelayMs")]
        public int XpateStatusRequestRetriesDelayMs { get; set; }
    }
}
