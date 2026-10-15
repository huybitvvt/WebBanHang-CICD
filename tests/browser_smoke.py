"""Optional browser verification. Requires Python + Playwright + installed Microsoft Edge.
Run against a dedicated local demo database; creates sample records.
"""
import re
from pathlib import Path
from playwright.sync_api import sync_playwright

BASE = 'http://localhost:5080/webbanhang'
EVIDENCE = Path(__file__).resolve().parents[1] / 'docs' / 'evidence'
EVIDENCE.mkdir(parents=True, exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel='msedge', headless=True)
    page = browser.new_page(viewport={'width': 1440, 'height': 1000}, device_scale_factor=1)
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    assert page.goto(BASE).status == 200
    page.screenshot(path=str(EVIDENCE / '01-tong-quan-desktop.png'), full_page=True)
    assert page.locator('link[rel=stylesheet]').get_attribute('href').startswith('/webbanhang/')
    page.get_by_role('link', name='Danh mục', exact=False).first.click()
    page.get_by_role('link', name='+ Thêm danh mục').click()
    page.get_by_label('Tên danh mục').fill('Danh mục demo browser')
    page.get_by_label('Mô tả').fill('Dữ liệu kiểm tra thao tác trình duyệt')
    page.get_by_role('button', name='Lưu danh mục').click()
    assert page.get_by_role('status').inner_text() == 'Đã lưu danh mục.'

    page.goto(BASE + '/Products/Create')
    page.get_by_label('Tên sản phẩm').fill('Bàn phím demo browser')
    page.get_by_label('Danh mục', exact=True).select_option(label='Danh mục demo browser')
    page.get_by_label('Giá bán (VNĐ)').fill('100000')
    page.get_by_label('Tồn kho').fill('10')
    page.get_by_role('button', name='Lưu sản phẩm').click()
    row = page.get_by_role('row').filter(has_text='Bàn phím demo browser')
    product_id = re.search(r'/Edit/(\d+)', row.get_by_role('link', name='Sửa').get_attribute('href')).group(1)
    page.screenshot(path=str(EVIDENCE / '02-san-pham.png'), full_page=True)

    page.goto(BASE + '/Orders/Create')
    page.get_by_label('Khách hàng').fill('Nguyễn Văn An')
    page.get_by_label('Số điện thoại').fill('0901234567')
    page.get_by_label('Sản phẩm', exact=True).select_option(product_id)
    page.get_by_label('Số lượng').fill('2')
    page.get_by_label('Giảm giá (%)').fill('10')
    # Add/remove and reindex a second line before saving.
    page.get_by_role('button', name='+ Thêm dòng').click()
    assert page.locator('.order-line').count() == 2
    page.locator('.remove-line').last.click()
    assert page.locator('.order-line').count() == 1
    page.get_by_role('button', name='Tạo đơn hàng', exact=True).click()
    assert '180.000 đ' in page.locator('.grand-total').inner_text()
    details_url = page.url
    page.screenshot(path=str(EVIDENCE / '03-don-hang-180000.png'), full_page=True)

    page.goto(BASE + '/Products')
    row = page.get_by_role('row').filter(has_text='Bàn phím demo browser')
    assert '8 còn lại' in row.inner_text()
    page.goto(details_url)
    page.once('dialog', lambda dialog: dialog.accept())
    page.get_by_role('button', name='Hủy đơn hàng', exact=True).click()
    assert 'Đã hủy' in page.locator('.badge.large').inner_text()
    page.goto(BASE + '/Products')
    assert '10 còn lại' in page.get_by_role('row').filter(has_text='Bàn phím demo browser').inner_text()

    # Verify edit/delete for a product that has no order history.
    page.goto(BASE + '/Products/Create')
    page.get_by_label('Tên sản phẩm').fill('Sản phẩm tạm để xóa')
    page.get_by_label('Danh mục', exact=True).select_option(label='Danh mục demo browser')
    page.get_by_label('Giá bán (VNĐ)').fill('200000')
    page.get_by_label('Tồn kho').fill('3')
    page.get_by_role('button', name='Lưu sản phẩm').click()
    row = page.get_by_role('row').filter(has_text='Sản phẩm tạm để xóa')
    row.get_by_role('link', name='Sửa').click()
    page.get_by_label('Tên sản phẩm').fill('Sản phẩm tạm đã sửa')
    page.get_by_role('button', name='Lưu sản phẩm').click()
    row = page.get_by_role('row').filter(has_text='Sản phẩm tạm đã sửa')
    page.once('dialog', lambda dialog: dialog.accept())
    row.get_by_role('button', name='Xóa', exact=True).click()
    assert page.get_by_role('row').filter(has_text='Sản phẩm tạm đã sửa').count() == 0

    mobile = browser.new_page(viewport={'width': 375, 'height': 812}, reduced_motion='reduce')
    mobile.goto(BASE)
    assert mobile.evaluate('document.documentElement.scrollWidth <= innerWidth')
    mobile.screenshot(path=str(EVIDENCE / '04-tong-quan-mobile.png'), full_page=True)
    mobile.goto(BASE + '/Orders/Create')
    assert mobile.evaluate('document.documentElement.scrollWidth <= innerWidth')
    mobile.screenshot(path=str(EVIDENCE / '05-tao-don-mobile.png'), full_page=True)
    assert not errors, errors
    browser.close()
print('PASS: category create; product create/edit/delete; order 180000; stock 10 -> 8 -> 10; dynamic lines; 375px layout; no JavaScript errors.')
