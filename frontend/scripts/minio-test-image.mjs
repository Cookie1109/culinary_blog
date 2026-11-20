import { execFile } from 'node:child_process'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { promisify } from 'node:util'

const run = promisify(execFile)
const context = fileURLToPath(new URL('../../infra/testing/', import.meta.url))

// Docker Hub no longer serves minio/minio, so build the checksum-verified image the backend
// fixtures already use. The tag matches MinioTestImage.Name in the integration tests.
export const MINIO_TEST_IMAGE = 'culinary-blog-minio-test:2025-09-07-v2'

let prepared

export function ensureMinioTestImage() {
  prepared ??= run('docker', ['image', 'inspect', MINIO_TEST_IMAGE]).catch(() =>
    run('docker', [
      'build',
      '--tag',
      MINIO_TEST_IMAGE,
      '--file',
      path.join(context, 'minio.Dockerfile'),
      context,
    ]),
  )
  return prepared
}
