import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SqlLoader } from './sql-loader';

describe('SqlLoader', () => {
  let component: SqlLoader;
  let fixture: ComponentFixture<SqlLoader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SqlLoader]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SqlLoader);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
