using System.ComponentModel.DataAnnotations;

namespace XFit.Utilities.Enums
{

    public enum ApiResultStatusCode
    {
        [Display(Name = "عملیات با موفقیت انجام شد")]
        Success = 0,

        [Display(Name = "مشکلی پیش آمده. لطفا بعدا دوباره امتحان کنید")]
        ServerError = 1,

        [Display(Name = "ورودی ها معتبر نمیباشد")]
        BadRequest = 2,

        [Display(Name = "یافت نشد")]
        NotFound = 3,

        [Display(Name = "ورودی نباید خالی باشد")]
        ListEmpty = 4,

        [Display(Name = "مشکلی پیش آمده. لطفا بعدا دوباره امتحان کنید")]
        LogicError = 5,

        [Display(Name = "احراز هویت مجوز")]
        UnAuthorized = 6,

        [Display(Name = "خطای عدم دسترسی")]
        Forbidden = 7,

        [Display(Name = "نقش مورد نظر یافت نشد")]
        RoleNotFound = 8,

        [Display(Name = "خطای درخواست تکراری")]
        Duplicated = 9,

        [Display(Name = "اندازه کاراکتر های ورودی بیش از اندازه میباشد")]
        Length = 10,

        [Display(Name = "داده های OAuth اشتباه است!")]
        OAuth = 11,

        [Display(Name = "اکانت کاربر مسدود میباشد")]
        Ban = 12,

        [Display(Name = "کد کپچا نادرست میباشد")]
        InvalidCaptcha = 13,

        [Display(Name = "فیلد اجباری باید پر شود")]
        Required = 14,

        [Display(Name = "تعداد درخواست های متوالی زیادی داده شده است. لطفا بعدا دوباره امتحان کنید")]
        TooManyRequests = 15,


        [Display(Name = "کد تایید منقضی شده است لطفا مجددا تلاش کنید")]
        ExpireVerification = 16,

    }
}

