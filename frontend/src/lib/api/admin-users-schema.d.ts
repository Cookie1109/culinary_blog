export interface paths {
  '/api/v1/admin/users': {
    parameters: {
      query?: never
      header?: never
      path?: never
      cookie?: never
    }
    /**
     * List users (Admin)
     * @description Private user directory. Search matches literal text in name/email (max 100 characters). Role is Admin or Author. Page defaults to 1; pageSize defaults to 20 and ranges from 1 to 100. Ordered by createdAt descending, then id. Users with multiple roles appear once with all their roles.
     */
    get: operations['ListAdminUsers']
    put?: never
    post?: never
    delete?: never
    options?: never
    head?: never
    patch?: never
    trace?: never
  }
}
export type webhooks = Record<string, never>
export interface components {
  schemas: {
    PageEnvelopeOfAdminUserDto: {
      data: components['schemas']['AdminUserDto'][]
      meta: components['schemas']['PageMeta']
    }
    AdminUserDto: {
      id: string
      displayName: string
      email: null | string
      roles: string[]
      isActive: boolean
      /** Format: date-time */
      createdAt: string
    }
    PageMeta: {
      /** Format: int32 */
      page: number | string
      /** Format: int32 */
      pageSize: number | string
      /** Format: int32 */
      total: number | string
      /** Format: int32 */
      totalPages: number | string
      hasNextPage: boolean
      hasPreviousPage: boolean
    }
    ProblemDetails: {
      type?: null | string
      title?: null | string
      /** Format: int32 */
      status?: null | number | string
      detail?: null | string
      instance?: null | string
    }
  }
  responses: never
  parameters: never
  requestBodies: never
  headers: never
  pathItems: never
}
export type $defs = Record<string, never>
export interface operations {
  ListAdminUsers: {
    parameters: {
      query?: {
        search?: string
        role?: string
        page?: number | string
        pageSize?: number | string
      }
      header?: never
      path?: never
      cookie?: never
    }
    requestBody?: never
    responses: {
      /** @description OK */
      200: {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/json': components['schemas']['PageEnvelopeOfAdminUserDto']
        }
      }
      /** @description Unauthorized */
      401: {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/problem+json': components['schemas']['ProblemDetails']
        }
      }
      /** @description Forbidden */
      403: {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/problem+json': components['schemas']['ProblemDetails']
        }
      }
      /** @description Semantically invalid field/query data or incomplete recipe content. Field validation errors include errors; version conflicts use409, malformed requests and missing If-Match use400. */
      422: {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/problem+json': {
            type?: null | string
            title?: null | string
            /** Format: int32 */
            status?: null | number | string
            detail?: null | string
            instance?: null | string
          }
        }
      }
      /** @description Validation, authentication, authorization, concurrency or rate-limit error. The body also includes code and traceId. */
      '4XX': {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/problem+json': {
            type?: null | string
            title?: null | string
            /** Format: int32 */
            status?: null | number | string
            detail?: null | string
            instance?: null | string
          }
        }
      }
      /** @description Server error (RFC 7807 Problem Details). */
      '5XX': {
        headers: {
          [name: string]: unknown
        }
        content: {
          'application/problem+json': {
            type?: null | string
            title?: null | string
            /** Format: int32 */
            status?: null | number | string
            detail?: null | string
            instance?: null | string
          }
        }
      }
    }
  }
}
