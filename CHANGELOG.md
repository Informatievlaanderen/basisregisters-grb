## [2.0.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v2.0.1...v2.0.2) (2024-04-08)


### Bug Fixes

* enable authorization ([af00ad1](https://github.com/informatievlaanderen/basisregisters-grb/commit/af00ad1ef9b8cf487888d8978b3aacfd06f33aed))

## [2.0.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v2.0.0...v2.0.1) (2024-04-08)

# [2.0.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.8...v2.0.0) (2024-03-15)


### Features

* move to dotnet 8.0.2 ([153a7b3](https://github.com/informatievlaanderen/basisregisters-grb/commit/153a7b317fe920ee5c280a911296c07e6981c9e0))


### BREAKING CHANGES

* move to dotnet 8.0.2

## [1.9.8](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.7...v1.9.8) (2024-02-08)


### Bug Fixes

* **bump:** ci dependency in workflow ([aabc2cd](https://github.com/informatievlaanderen/basisregisters-grb/commit/aabc2cddbf0ead9efbd3a0cfe2fcce0e6c8a28c9))

## [1.9.7](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.6...v1.9.7) (2024-02-08)


### Bug Fixes

* **bump:** ci new pipeline ([ebc8a04](https://github.com/informatievlaanderen/basisregisters-grb/commit/ebc8a0485d5cb9ddbe51a855fa666fe40077adc2))

## [1.9.6](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.5...v1.9.6) (2024-01-12)


### Bug Fixes

* Can't use Linq Any when using EF FromSqlRaw ([c7e8c31](https://github.com/informatievlaanderen/basisregisters-grb/commit/c7e8c31451a3fc6d1acf472ab66aa6f6e6d7f537))

## [1.9.5](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.4...v1.9.5) (2023-11-30)


### Bug Fixes

* change upload dbase schema field lengths ([029ee57](https://github.com/informatievlaanderen/basisregisters-grb/commit/029ee57357c63932a59311dc2d7085d030dde35c))

## [1.9.4](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.3...v1.9.4) (2023-09-14)


### Bug Fixes

* post notification when unhandled exception in job processor ([3d09b43](https://github.com/informatievlaanderen/basisregisters-grb/commit/3d09b4308587148b0921519e9613537c264061cf))

## [1.9.3](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.2...v1.9.3) (2023-09-13)


### Bug Fixes

* change notification service name for job processor notifications ([b90c9c7](https://github.com/informatievlaanderen/basisregisters-grb/commit/b90c9c768cdf938bb16fd8b898e7106593cf3810))
* logging error upload grb ([0089e08](https://github.com/informatievlaanderen/basisregisters-grb/commit/0089e08b623e275ccb3895b46391b5e47e3b56d4))

## [1.9.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.1...v1.9.2) (2023-08-23)


### Bug Fixes

* open datareader + divide by zero exception ([1fbc4b5](https://github.com/informatievlaanderen/basisregisters-grb/commit/1fbc4b5f3bf8f82bc745f879c1f0673a08f22998))

## [1.9.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.9.0...v1.9.1) (2023-08-23)


### Performance Improvements

* monitor in parallel ([e69a5aa](https://github.com/informatievlaanderen/basisregisters-grb/commit/e69a5aa43bf47f5edb50478d12f3e47d786fb572))
* process in parallel ([07bd145](https://github.com/informatievlaanderen/basisregisters-grb/commit/07bd145c9a7f1eeff112fd0b230be21aec135857))

# [1.9.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.8.2...v1.9.0) (2023-08-22)


### Features

* expand getjobrecords with archived jobrecords ([a2c4d33](https://github.com/informatievlaanderen/basisregisters-grb/commit/a2c4d334fd54dc46bd2fb5aa0dbc401b2f2b700b))

## [1.8.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.8.1...v1.8.2) (2023-08-21)


### Bug Fixes

* append error message with grid ([00ad27a](https://github.com/informatievlaanderen/basisregisters-grb/commit/00ad27af14edc3726fa336a905add3bbd33a8a4a))
* not execute job when previous job in error ([443e077](https://github.com/informatievlaanderen/basisregisters-grb/commit/443e0779fe3b4fd0583394234e32864b1956fb5a))

## [1.8.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.8.0...v1.8.1) (2023-08-18)


### Bug Fixes

* record error formatting ([7db79b3](https://github.com/informatievlaanderen/basisregisters-grb/commit/7db79b38f65e6da3ed8e0ff1eb3121ca77ebefbf))

# [1.8.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.6...v1.8.0) (2023-08-18)


### Features

* add errorcode to jobrecords archive ([1b272c9](https://github.com/informatievlaanderen/basisregisters-grb/commit/1b272c954ded4ef5a9fe407e78c5ac8b136ffe17))

## [1.7.6](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.5...v1.7.6) (2023-08-14)


### Bug Fixes

* add errors to ticket when errored job is cancelled ([e9f648e](https://github.com/informatievlaanderen/basisregisters-grb/commit/e9f648e71258dcb94ccad72e0ef57c1d655869e0))
* add http status code to not found error in httpproxy ([ca5d888](https://github.com/informatievlaanderen/basisregisters-grb/commit/ca5d888825f2c9e86a0c0946747e76627cd92721))
* error handling + warnings to completed ticket ([469c1fe](https://github.com/informatievlaanderen/basisregisters-grb/commit/469c1fed4618da801f68a961fc7a4778487fdac0))
* get ticket errors from ticket when cancelling job ([88705f5](https://github.com/informatievlaanderen/basisregisters-grb/commit/88705f54568fe40c3635ce80a3c827968b4210ef))

## [1.7.5](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.4...v1.7.5) (2023-08-02)


### Bug Fixes

* change gebouw_ALL.zip schema col from IDN to GRBIDN ([1df4be8](https://github.com/informatievlaanderen/basisregisters-grb/commit/1df4be8f43b25dd75808e02930376b6e1a4d295b))

## [1.7.4](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.3...v1.7.4) (2023-08-02)


### Bug Fixes

* jobResult IDN column to GRBIDN ([261eca5](https://github.com/informatievlaanderen/basisregisters-grb/commit/261eca55b1d7188351bf2b25168e1bf9f8dfc082))

## [1.7.3](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.2...v1.7.3) (2023-07-28)


### Bug Fixes

* change jobresult column names to IDN, GRBOBJECT, GRID ([0972d97](https://github.com/informatievlaanderen/basisregisters-grb/commit/0972d9722c72fd42241e36c0a6c883137a84cc44))

## [1.7.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.1...v1.7.2) (2023-07-26)


### Bug Fixes

* errormessage canceljob and jobresults ([81734bb](https://github.com/informatievlaanderen/basisregisters-grb/commit/81734bb5160413a103b5673e1f6180f51047bbec))
* get buildingreadurl from config ([ec86f89](https://github.com/informatievlaanderen/basisregisters-grb/commit/ec86f894b31206dace6a50779bd777244420cbf8))

## [1.7.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.7.0...v1.7.1) (2023-07-26)


### Bug Fixes

* grb importer missing file errors ([2e368d7](https://github.com/informatievlaanderen/basisregisters-grb/commit/2e368d77f7acc5597913bb68dca9389fe676ca56))

# [1.7.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.6.1...v1.7.0) (2023-07-26)


### Features

* add grbobject and change schema names in jobresults ([74df982](https://github.com/informatievlaanderen/basisregisters-grb/commit/74df982be1b0f577e76f0b670a8059f08ec3b4f1))

## [1.6.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.6.0...v1.6.1) (2023-07-25)


### Bug Fixes

* instantiate ticketError using different ctor + added more specific error message ([1b68bd0](https://github.com/informatievlaanderen/basisregisters-grb/commit/1b68bd020b609b3378dd2138d757dc0f3fd3e61d))

# [1.6.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.5.0...v1.6.0) (2023-07-20)


### Features

* add errorcode to jobrecord ([ffa454d](https://github.com/informatievlaanderen/basisregisters-grb/commit/ffa454dfe5f1d03adffc9b87083fcccacea5e2e4))

# [1.5.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.4.2...v1.5.0) (2023-07-13)


### Features

* job is cancellable when in error but without job records ([62fa608](https://github.com/informatievlaanderen/basisregisters-grb/commit/62fa608884ce25b9187ac2e2f24adba38a2d3882))

## [1.4.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.4.1...v1.4.2) (2023-07-13)


### Bug Fixes

* message notification ([1229163](https://github.com/informatievlaanderen/basisregisters-grb/commit/1229163df68756bfa5d165f03b085741e93d2c88))
* set job record status upon creating ([c0e6a63](https://github.com/informatievlaanderen/basisregisters-grb/commit/c0e6a6300f103c59f2839622c88e134b556f978e))

## [1.4.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.4.0...v1.4.1) (2023-07-10)


### Bug Fixes

* fix appsettings ([72e7ea3](https://github.com/informatievlaanderen/basisregisters-grb/commit/72e7ea31d98273eab789e874bdb7a90b487b0462))

# [1.4.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.3.0...v1.4.0) (2023-07-07)


### Bug Fixes

* set severity to good on completed jobs and camelcasing for jsonconvert ([a9601c4](https://github.com/informatievlaanderen/basisregisters-grb/commit/a9601c41a9ef74239cbf24440092f40fb0c5d325))


### Features

* add notifications ([f7d7fcb](https://github.com/informatievlaanderen/basisregisters-grb/commit/f7d7fcb6bcc570bf59d7e0e19a749a8109038dfa))

# [1.3.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.2.0...v1.3.0) (2023-07-03)


### Features

* add abstractions ([fc6432f](https://github.com/informatievlaanderen/basisregisters-grb/commit/fc6432f335134671c11d56c8e3a5d989b9d0ac26))

# [1.2.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.1.0...v1.2.0) (2023-07-03)


### Bug Fixes

* set paket.tempalte to content copy always ([055c58c](https://github.com/informatievlaanderen/basisregisters-grb/commit/055c58c8fc5df42cf3db5d61b0071a9ea37ee17c))


### Features

* extend endpoints for ops ([b4e73ca](https://github.com/informatievlaanderen/basisregisters-grb/commit/b4e73cabf034131850c89f165a6de2e9094e4d41))

# [1.1.0](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.5...v1.1.0) (2023-06-12)


### Bug Fixes

* limit authorization to DV on ResolveJobRecordError ([beaf0e8](https://github.com/informatievlaanderen/basisregisters-grb/commit/beaf0e8a5acf64255515e17c9b3394ab3e6481ba))
* name of GetJob controller action ([4106acb](https://github.com/informatievlaanderen/basisregisters-grb/commit/4106acbd0490b55162982f24731fdec0afc91151))
* rename JobRecordStatus.Complete to JobRecordStatus.Completed ([1bee797](https://github.com/informatievlaanderen/basisregisters-grb/commit/1bee7976ed43e30d69bdf36182f14152827dd622))
* return api errors in Dutch ([c23f4ac](https://github.com/informatievlaanderen/basisregisters-grb/commit/c23f4ac7c662cff47e247f278295e521433d288b))


### Features

* get active jobs ([a39f694](https://github.com/informatievlaanderen/basisregisters-grb/commit/a39f694b029c159469fdfa32fe5f5498c04cd5f0))
* get job records ([e7fb55b](https://github.com/informatievlaanderen/basisregisters-grb/commit/e7fb55b6761c78c3c7f6575d88f72842a7d11e13))
* resolve job record error ([af93428](https://github.com/informatievlaanderen/basisregisters-grb/commit/af9342883f63f50d864c5d239e6a89ef0ec21d04))
* return job details additional to job records ([de2eba3](https://github.com/informatievlaanderen/basisregisters-grb/commit/de2eba383e28ab2051c64cf6e291e7a1c5faeb68))
* store record number from dbase file on jobrecord ([e720f01](https://github.com/informatievlaanderen/basisregisters-grb/commit/e720f011c81741eea5830746c2fabacb34adf8f6))

## [1.0.5](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.4...v1.0.5) (2023-06-06)


### Bug Fixes

* ensure job records are processed in order ([2c31655](https://github.com/informatievlaanderen/basisregisters-grb/commit/2c316555044ba8fea7f5de199c53cd57dede186f))

## [1.0.4](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.3...v1.0.4) (2023-06-05)


### Bug Fixes

* reference Be.Vlaanderen.Basisregisters.GrAr.Provenance in job processor ([051df13](https://github.com/informatievlaanderen/basisregisters-grb/commit/051df1318003f3195f55a122214c491d3754c69e))

## [1.0.3](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.2...v1.0.3) (2023-06-02)


### Bug Fixes

* grp api deploy pipeline ([0d481bd](https://github.com/informatievlaanderen/basisregisters-grb/commit/0d481bde15977b933d3f7cf34d6d52b60da58aa8))

## [1.0.2](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.1...v1.0.2) (2023-06-02)


### Bug Fixes

* ci/cd prd pipeline ([9601019](https://github.com/informatievlaanderen/basisregisters-grb/commit/9601019ef23c7bbcb9f172d70cd6e39526facf65))

## [1.0.1](https://github.com/informatievlaanderen/basisregisters-grb/compare/v1.0.0...v1.0.1) (2023-06-02)


### Bug Fixes

* use correct ecr registry ([baf1ab0](https://github.com/informatievlaanderen/basisregisters-grb/commit/baf1ab0648ab43a463cca717046e3dbac82b952c))

# 1.0.0 (2023-06-02)


### Bug Fixes

* add db schema to JobRecordsArchiver sql statements ([9b4aa1b](https://github.com/informatievlaanderen/basisregisters-grb/commit/9b4aa1bee3c2f909a3a99852b405d66cf98f8ba3))
* already an open DataReader associated with this Connection + migration tablename ([a856feb](https://github.com/informatievlaanderen/basisregisters-grb/commit/a856febac84cf3cbff263468c75caf966c16d2ae))
* BackOfficeApiResult IsSuccess evaluation ([c788f92](https://github.com/informatievlaanderen/basisregisters-grb/commit/c788f9233d4de180f5d6060cbe7018da3159076f))
* build ([705227e](https://github.com/informatievlaanderen/basisregisters-grb/commit/705227efd76d557df8c545f0c48ef691fcdb743d))
* complete ticket when grb job is cancelled ([37c97b5](https://github.com/informatievlaanderen/basisregisters-grb/commit/37c97b5ab9a346872b4ca49077e6152b5aa47c0e))
* correct ecr registry in release.yaml ([48636a6](https://github.com/informatievlaanderen/basisregisters-grb/commit/48636a65dcc32bca00fb37b9a96785ede7a74d52))
* don't use parallelization in jobrecordmonitor ([b125f5a](https://github.com/informatievlaanderen/basisregisters-grb/commit/b125f5a21888bfc6053ffb390b9006294446497d))
* error code null in errorWarningEvaluator ([7036ff7](https://github.com/informatievlaanderen/basisregisters-grb/commit/7036ff7d9a115b93daa0a855f165e546ab295a1b))
* job results should use public api url ([e318d71](https://github.com/informatievlaanderen/basisregisters-grb/commit/e318d7123329da648e32dee063262d6da5f36501))
* make build.sh executable ([b0e20ec](https://github.com/informatievlaanderen/basisregisters-grb/commit/b0e20ece7cbcceacdb6d9a4f588334324394c318))
* reference AggregateSource.ExplicitRouting ([3396f3f](https://github.com/informatievlaanderen/basisregisters-grb/commit/3396f3fda3364882603552a629a129c1be755080))
* register AmazonS3Client with configured region ([3f0e7e3](https://github.com/informatievlaanderen/basisregisters-grb/commit/3f0e7e331eeac80a3b9f4e9f10b7951621e26977))
* register grbApiOptions + routes backoffice api ([138dc8d](https://github.com/informatievlaanderen/basisregisters-grb/commit/138dc8dccb8db2fc38047a77ccfa861f2078d09a))
* remove redundant references ([d80c6ef](https://github.com/informatievlaanderen/basisregisters-grb/commit/d80c6ef5db01b8d277773caf14ee7fa3fb1eb6fd))
* run task with network configuration ([f22c7bc](https://github.com/informatievlaanderen/basisregisters-grb/commit/f22c7bcc4adb8783ea11040e0d8ea6d675ad9cd5))
* set grb results stream position to 0 before uploading ([2487690](https://github.com/informatievlaanderen/basisregisters-grb/commit/2487690ca9fe24235b1058336932d4e3a37331eb))
* support binary file types in s3 getblobasync ([abaf154](https://github.com/informatievlaanderen/basisregisters-grb/commit/abaf1547d1b4557c56d4ebeda6e0b196f4ead9aa))
* trigger build ([05249de](https://github.com/informatievlaanderen/basisregisters-grb/commit/05249deff0b968c63eadc07d5374fe87d59abb3f))
* zip job results dbf file ([416aa00](https://github.com/informatievlaanderen/basisregisters-grb/commit/416aa0092b361e2b1c9c8bbeef1e8d05b55004a0))
