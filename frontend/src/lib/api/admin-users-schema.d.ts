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
    /** @description Carries page envelope data. */
    PageEnvelopeOfAdminUserDto: {
      /** @description The data supplied to this operation. */
      data: components['schemas']['AdminUserDto'][]
      /** @description The meta supplied to this operation. */
      meta: components['schemas']['PageMeta']
      /** @description The message supplied to this operation. */
      message?: null | string
    }
    /** @description Carries admin user dto data. */
    AdminUserDto: {
      /** @description The identifier of the requested resource. */
      id: string
      /** @description The display name supplied to this operation. */
      displayName: string
      /** @description The email supplied to this operation. */
      email: null | string
      /** @description The roles supplied to this operation. */
      roles: string[]
      /** @description The is active supplied to this operation. */
      isActive: boolean
      /**
       * Format: date-time
       * @description The UTC creation time.
       */
      createdAt: string
    }
    /** @description Carries page meta data. */
    PageMeta: {
      /**
       * Format: int32
       * @description The page supplied to this operation.
       */
      page: number | string
      /**
       * Format: int32
       * @description The page size supplied to this operation.
       */
      pageSize: number | string
      /**
       * Format: int32
       * @description The total supplied to this operation.
       */
      total: number | string
      /**
       * Format: int32
       * @description The total pages supplied to this operation.
       */
      totalPages: number | string
      /** @description The has next page supplied to this operation. */
      hasNextPage: boolean
      /** @description The has previous page supplied to this operation. */
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
          /** @description The binding shared IP quota and its remaining requests. */
          'X-RateLimit-Limit'?: number
          /** @description The binding shared IP quota and its remaining requests. */
          'X-RateLimit-Remaining'?: number
          /** @description Unix timestamp in seconds when the limiting window next admits a request. */
          'X-RateLimit-Reset'?: number
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
      /** @description Shared IP quota exceeded: global fixed 100/minute; auth exact sliding 10/minute; upload fixed 5/minute. Requests consume the global window as well. Headers describe the binding quota. Redis mirrors the shared PostgreSQL clock and ledger; quotas and headers remain enforced during Redis outage and recovery. */
      429: {
        headers: {
          /** @description The binding shared IP quota and its remaining requests. */
          'X-RateLimit-Limit'?: number
          /** @description The binding shared IP quota and its remaining requests. */
          'X-RateLimit-Remaining'?: number
          /** @description Unix timestamp in seconds when the limiting window next admits a request. */
          'X-RateLimit-Reset'?: number
          /** @description Seconds until retry (at least 1). */
          'Retry-After'?: number
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
