namespace Budget.Shared.Enums;

public enum FillAmounts
{
  NotSet = 0,

  [Display(Name = "Fill 100% Of Budget")] OneHundredPercent = 1,

  [Display(Name = "Fill 50% Of Budget")] FiftyPercent = 2,

  [Display(Name = "Fill To Budget")] FillToBudget = 3
}