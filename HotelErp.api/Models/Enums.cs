namespace HotelErp.Api.Models;

public enum RateRuleType { LengthOfStay, Weekend, Seasonal }
public enum BookingSource { Phone, WalkIn, Online }
public enum GovernmentIdType { NationalId, Passport, DrivingLicense }
public enum PaymentMethod { Cash, Card, MobileMoney, BankTransfer }
public enum RequestType { Housekeeping, Maintenance, RoomService, Laundry, Other }
public enum FolioLineType { RoomCharge, ServiceCharge, Tax, Payment, Adjustment }