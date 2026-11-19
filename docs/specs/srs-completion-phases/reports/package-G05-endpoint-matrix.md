# G05 — Endpoint → happy/error matrix

Nguồn: OpenAPI G04 hiện tại, SRS NFR-MAINT-002 trang43 và FR tương ứng. Đường dẫn trừ health có prefix `/api/v1` theo servers của contract; `/` là API root. Mỗi symbol chỉ rõ bài có assertion happy/error, không dùng tổng test để suy coverage. Gate `scripts/verify-endpoint-matrix.py --trx <full-integration.trx>` kiểm đủ operation và test đã chạy; review assertion vẫn cần đọc test.

Google: handler được thay trong HTTP adapter test để kiểm envelope và validation422, **không** xác nhận provider/signature/account linking; phase10 giữ ngoài phạm vi. Health: controlled IHealthCheck chứng minh200/503 và privacy, không thay failure drill thật của G06. Supporting GET root/release/maintenance không có business input error: negative case POST405, không tạo ngoại lệ acceptance. Categories GET negative429 dùng quota thật. Sitemap HTTP đọc artifact qua storage test; MinIO thật có regression MediaStorageTests. SRS/đính chính A/B/C không đổi.

| Operation | FR/NFR | Happy test | Error test |
|---|---|---|---|
| GET / | CONS-005 | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites |
| GET /release | CONS-005 | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites |
| GET /operations/maintenance | CONS-005 | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites | EndpointMatrixTests.SupportingReadEndpointsReturnDataAndRejectWrites |
| GET /sitemap.xml | FR-JOB-003 | EndpointMatrixTests.SitemapEndpointReturnsStoredXmlOrNotFound | EndpointMatrixTests.SitemapEndpointReturnsStoredXmlOrNotFound |
| POST /auth/register | FR-AUTH-001 | AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure | AuthLifecycleTests.RegistrationDuplicateTtlsGenericLoginAndSafeProfileMatchLocalContract |
| POST /auth/login | FR-AUTH-002 | AuthLifecycleTests.RegistrationDuplicateTtlsGenericLoginAndSafeProfileMatchLocalContract | AuthLifecycleTests.RegistrationDuplicateTtlsGenericLoginAndSafeProfileMatchLocalContract |
| POST /auth/google | FR-AUTH-003 | EndpointMatrixTests.GoogleHttpAdapterReturnsSessionAndRejectsInvalidBodyWithoutCallingProvider | EndpointMatrixTests.GoogleHttpAdapterReturnsSessionAndRejectsInvalidBodyWithoutCallingProvider |
| POST /auth/refresh | FR-AUTH-004 | AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure | AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure |
| POST /auth/logout | FR-AUTH-005 | AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure | AuthLifecycleTests.LogoutChecksBearerOwnerAndRejectsInvalidSignaturesBeforeRevoking |
| POST /auth/change-password | FR-AUTH-008 | PasswordLifecycleTests.ChangeRequiresCurrentPasswordAndRevokesEverySession | PasswordLifecycleTests.ChangeRequiresCurrentPasswordAndRevokesEverySession |
| POST /auth/forgot-password | FR-AUTH-008 | PasswordLifecycleTests.ForgotDoesNotDiscloseUnknownAccountsAndResetTokenIsBoundAndSingleUse | PasswordLifecycleTests.ForgotDoesNotDiscloseUnknownAccountsAndResetTokenIsBoundAndSingleUse |
| POST /auth/reset-password | FR-AUTH-008 | PasswordLifecycleTests.ForgotDoesNotDiscloseUnknownAccountsAndResetTokenIsBoundAndSingleUse | PasswordLifecycleTests.ForgotDoesNotDiscloseUnknownAccountsAndResetTokenIsBoundAndSingleUse |
| GET /auth/me | FR-AUTH-006 | AuthLifecycleTests.RegistrationDuplicateTtlsGenericLoginAndSafeProfileMatchLocalContract | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| PATCH /auth/me | FR-AUTH-007 | AuthLifecycleTests.ProfileAvatarCanBeUpdatedAndClearedAndInvalidUrlsDoNotPartiallyUpdateProfile | AuthLifecycleTests.ProfileAvatarCanBeUpdatedAndClearedAndInvalidUrlsDoNotPartiallyUpdateProfile |
| GET /categories | FR-CAT-001 | RecipeDiscoveryApiTests.PublicCategoriesAreAlphabeticalEvenWhenAdminOrderIndexesAreReversed | AuthRateLimitTests.GlobalFixedQuotaIsOneHundredAcrossTwoInstances |
| POST /categories | FR-CAT-003 | RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts | RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts |
| GET /categories/{slug} | FR-CAT-002 | RecipeDiscoveryApiTests.SortAliasesMatchCanonicalOrderRejectConflictsAndVaryCategoryCache | RecipeDiscoveryApiTests.SortAliasesMatchCanonicalOrderRejectConflictsAndVaryCategoryCache |
| PUT /categories/{id} | FR-CAT-004 | RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts | RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts |
| DELETE /categories/{id} | FR-CAT-005 | AdminAuditLogsApiTests.AdminRequestReturnsAuditLogsAndTelemetryMetadata | RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts |
| GET /admin/recipes | FR-RCP-001 | RecipeCoreApiTests.ManagedSearchFiltersBeforePagingAndPreservesOwnershipAndAdminScope | RecipeCoreApiTests.P7OwnershipRoleEscalationAndAdminOverrideAreEnforced |
| GET /admin/recipes/{id} | FR-RCP-002 | RecipeCoreApiTests.P7OwnershipRoleEscalationAndAdminOverrideAreEnforced | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| GET /admin/audit-logs | NFR-SEC-006 | AdminAuditLogsApiTests.AdminRequestReturnsAuditLogsAndTelemetryMetadata | AdminAuditLogsApiTests.AuthorRequestIsRejectedWithForbidden |
| GET /recipes | FR-RCP-001 | RecipeDiscoveryApiTests.ListAppliesAllFiltersSortAndPaginationToPublishedRecipesOnly | RecipeDiscoveryApiTests.SortAliasesMatchCanonicalOrderRejectConflictsAndVaryCategoryCache |
| POST /recipes | FR-RCP-003 | RecipeCoreApiTests.CreatePersistsNestedCompositionAndRejectsInvalidChildrenWithoutPartialWrites | RecipeCoreApiTests.CreatePersistsNestedCompositionAndRejectsInvalidChildrenWithoutPartialWrites |
| GET /recipes/search | FR-SRCH-001/002/003/004 | RecipeDiscoveryApiTests.SearchIsAccentInsensitiveRankedAndNeverReturnsDrafts | RecipeDiscoveryApiTests.KeywordSearchRejectsMissingOrShortQueries |
| GET /recipes/{slug} | FR-RCP-002 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle |
| PUT /recipes/{id} | FR-RCP-004 | ContentUpdateContractTests.PartialUpdatesPreserveOmittedFieldsClearNullableValuesAndRejectInvalidValuesAtomically | RecipeCoreApiTests.StaleRecipeUpdateReturnsConflictAndCurrentEtag |
| DELETE /recipes/{id} | FR-RCP-007 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| GET /me/recipes | FR-RCP-008 | RecipeCoreApiTests.ManagedSearchFiltersBeforePagingAndPreservesOwnershipAndAdminScope | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| GET /me/recipes/{id} | FR-RCP-002 | RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous | RecipeCoreApiTests.P7OwnershipRoleEscalationAndAdminOverrideAreEnforced |
| POST /recipes/{id}/images | FR-FILE-001 | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate |
| PATCH /recipes/{id}/images/{imageId} | FR-FILE-001 | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| DELETE /recipes/{id}/images/{imageId} | FR-FILE-003 | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate |
| POST /recipes/{id}/ingredients | FR-RCP-009 | RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous | RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous |
| PUT /recipes/{id}/ingredients/{ingredientId} | FR-RCP-009 | ContentUpdateContractTests.PartialUpdatesPreserveOmittedFieldsClearNullableValuesAndRejectInvalidValuesAtomically | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| DELETE /recipes/{id}/ingredients/{ingredientId} | FR-RCP-009 | ContentUpdateContractTests.ChildDeletesAdvanceVersionRejectStaleWritesAndKeepStepsContinuous | ContentUpdateContractTests.ChildDeletesAdvanceVersionRejectStaleWritesAndKeepStepsContinuous |
| POST /recipes/{id}/steps | FR-RCP-010 | RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| PUT /recipes/{id}/steps/{stepId} | FR-RCP-010 | RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| DELETE /recipes/{id}/steps/{stepId} | FR-RCP-010 | ContentUpdateContractTests.ChildDeletesAdvanceVersionRejectStaleWritesAndKeepStepsContinuous | ContentUpdateContractTests.ChildDeletesAdvanceVersionRejectStaleWritesAndKeepStepsContinuous |
| GET /media/{imageId}/{variant} | FR-FILE-001 | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate | RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate |
| PATCH /recipes/{id}/publish | FR-RCP-005 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| PATCH /recipes/{id}/unpublish | FR-RCP-005 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| PATCH /recipes/{id}/archive | FR-RCP-006 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| PATCH /recipes/{id}/unarchive | FR-RCP-006 | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle | EndpointMatrixTests.ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources |
| GET /health | NFR-REL-002 | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails |
| GET /health/live | NFR-REL-002 | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails |
| GET /health/ready | NFR-REL-002 | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails | HealthEndpointMatrixTests.HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails |
