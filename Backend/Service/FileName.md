ÇáÊÑÊíÈ Çááí ÊßÊÈ Èíå
ÇßÊÈ ãßæäÇÊ ÇáÜ feature ÈÇáÊÑÊíÈ ÇáÊÇáí:
1. DTO (ÇáÎÇÑÌ): ÍÏÏ Ôßá ÇáÈíÇäÇÊ ÇáäåÇÆí Çááí ÇáÜ UI ãÍÊÇÌå ÇáÃæá.
2. Query / Command (ÇáÏÇÎá): ÚÑøİ ÇáÈíÇäÇÊ ÇáãØáæÈÉ áÊäİíĞ ÇáÚãáíÉ.
3. Validator: ÃÖİ ŞæÇÚÏ ÇáÊÍŞŞ ãä ÇáãÏÎáÇÊ¡ áæ İíå ŞæÇÚÏ ÊÓÊÏÚí
   Ğáß.
4. Handler: ÇÑÈØ ÇáãÏÎáÇÊ ÈÇáÜ Repository Ãæ ÇáÎÏãÇÊ¡ æäİøĞ ãäØŞ
   ÇáÚãáíÉ.
5. Controller Endpoint: ÇÓÊŞÈá ÇáØáÈ¡ æÇÓÊÎÑÌ ÇáÈíÇäÇÊ ãä ãÕÏÑåÇ¡
   Ëã ÇÓÊÏÚö ÇáÜ Mediator æÃÑÌÚ ÇáÇÓÊÌÇÈÉ ÇáãäÇÓÈÉ.


ãËÇá ÓÑíÚ: GetMyOrders
- Inputs: pageNumber æ`pageSize` ãä Query String.
- Token data: userId ãä ÇáãÓÊÎÏã ÇáãÕÇÏŞ Úáíå.
- Output DTO: ŞÇÆãÉ ÇáØáÈÇÊ ÈÇáÍŞæá Id, Status, Date,
  ItemsCount, Total.
- Database: ÇÓÊÏÚÇÁ GetPagedByCustomerAsync ãÚ userId æÈíÇäÇÊ
  ÇáÜ pagination.
- Validation / Business Rules: ÇáÊÃßÏ ãä ÕáÇÍíÉ Şíã paginationº
  æİáÊÑÉ ÇáØáÈÇÊ ÈÇÓÊÎÏÇã userId áÖãÇä ÅÑÌÇÚ ØáÈÇÊ ÇáãÓÊÎÏã ÇáÍÇáí
  İŞØ.
- Failure handling: ÊÍÏíÏ ØÑíŞÉ ÇáÊÚÇãá ãÚ ÇáãÏÎáÇÊ ÛíÑ ÇáÕÍíÍÉ Ãæ
  ÃÎØÇÁ ÇáæÕæá ááÈíÇäÇÊ. áæ ãİíÔ ÍÇáÉ İÔá ãÊæŞÚÉ İí ãäØŞ ÇáÜ query¡ ãÇ
  ÊÖíİÔ Exceptions Ãæ Failure Results ãä ÛíÑ ÏÇÚò.
ãáÇÍÙÉ: ãÔ ßá Query ãÍÊÇÌÉ Validator Ãæ Result.Failure.
ÇÎÊÇÑ ÂáíÉ ÇáÊÍŞŞ æÇáÊÚÇãá ãÚ ÇáÃÎØÇÁ ÍÓÈ ŞæÇÚÏ ÇáÜ feature ÇáİÚáíÉ¡
æãÇ ÊÖíİÔ ÊÚŞíÏ ãä ÛíÑ ÇÍÊíÇÌ.